using System;
using System.Collections.Generic;
using System.IO;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Default <see cref="IPlayerProfileService"/>.
    ///
    /// <para><b>Storage keys.</b> Every section's data is its own
    /// <see cref="Runtime.Persistence.IPersistenceService"/> key,
    /// <c>"GameFramework.PlayerData.{profileId}.{sectionId}"</c> - one already-versioned,
    /// already-migratable <c>Save</c>/<c>Load</c> call per section, exactly the shape Phase 2
    /// already supports for a single dedicated data class. A profile's metadata is a sibling key,
    /// <c>"...{profileId}.Meta"</c>, and the set of profile ids that exist is tracked separately
    /// under <c>"GameFramework.PlayerData.Index"</c> because <see cref="IPersistenceStorage"/> has
    /// no "list all keys" capability.</para>
    ///
    /// <para><b>Not applied to Phase 3/6/9's existing systems.</b> <c>SettingsService</c>,
    /// <c>EconomyService</c>, <c>InventoryService</c>, <c>ExperienceService</c>,
    /// <c>StatisticsService</c>, and <c>TutorialService</c> each already persist themselves
    /// directly against a fixed, non-profile-scoped <see cref="IPersistenceService"/> key (e.g.
    /// <c>"GameFramework.Progression.Economy"</c>). Retrofitting them to use profile-scoped keys
    /// would change every existing game's save file location and is a breaking migration outside
    /// this phase's scope (CLAUDE.md's Phase 13 brief, section 73's "explain the conflict, do not
    /// force a migration"). This service is new, additive infrastructure for a game's own sections
    /// going forward; a game that wants one of those six systems' data to live inside a profile can
    /// wrap it in a small <see cref="PlayerDataSection{TData}"/> adapter itself. See
    /// <c>Framework.md</c>'s Phase 13 section for the full discussion and the known-limitation this
    /// leaves.</para>
    ///
    /// <para><b>Corruption/backup.</b> Before overwriting a section's on-disk data,
    /// <see cref="EnableBackups"/> (on by default) copies what is currently stored there to a
    /// <c>".bak"</c> companion key. On load, this service deletes any stale <c>".corrupt"</c>
    /// marker for a key before calling <see cref="IPersistenceService.Load{TData}"/>, then checks
    /// whether a new one appears - <see cref="PersistenceService"/> only ever creates that marker
    /// when a load actually failed (see its own remarks), so this reliably distinguishes "this load
    /// just failed" from "no save exists yet" without needing Phase 2 to expose anything new. A
    /// failed section first tries its own <c>".bak"</c>, then falls back to
    /// <see cref="IPlayerDataSection.ResetToDefaults"/> - never left half-loaded, and the original
    /// corrupt bytes are still preserved under <c>".corrupt"</c> by Phase 2 itself.</para>
    ///
    /// <para><b>Concurrency.</b> Every public command is guarded by a single "busy" flag and
    /// rejected with <see cref="ProfileOperationResultKind.AlreadyActive"/> while another is in
    /// progress, including a call issued synchronously from inside this service's own event
    /// handlers - the same re-entrancy rule <c>UI.Navigation.INavigationService</c> already
    /// documents. Rapid dirty-driven autosave requests are coalesced (a new mutation reschedules the
    /// pending debounce timer rather than queuing another save); everything else is a synchronous,
    /// single-threaded operation, so there is no true overlapping-save race to resolve.</para>
    /// </summary>
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private const string LogCategory = "PlayerData";
        private const string KeyPrefix = "GameFramework.PlayerData.";
        private const string IndexKey = KeyPrefix + "Index";
        private const int IndexVersion = 1;
        private const string MetadataKeySuffix = ".Meta";
        private const int MetadataVersion = 1;
        private const string BackupSuffix = ".bak";
        private const string CorruptSuffix = ".corrupt";
        private const int ProfileSchemaVersion = 1;

        private readonly Dictionary<Type, Func<IPlayerDataSection>> _factoriesByType = new Dictionary<Type, Func<IPlayerDataSection>>();
        private readonly Dictionary<string, Type> _typeById = new Dictionary<string, Type>();
        private readonly List<string> _sectionIdOrder = new List<string>();
        private readonly Dictionary<string, List<ISaveMigration>> _pendingMigrationsBySectionId = new Dictionary<string, List<ISaveMigration>>();
        private readonly HashSet<string> _migrationsForwardedForKey = new HashSet<string>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ITimerService _timer;
        private ILoggingService _log;
        private ISceneService _scene;
        private PlayerDataLifecycleDriver _driver;

        private PlayerProfileIndexData _index = new PlayerProfileIndexData();
        private AutosavePolicy _autosavePolicy = AutosavePolicy.Default;
        private ITimerHandle _pendingDebounceTimer;
        private bool _busy;

        public ProfileState State { get; private set; } = ProfileState.Unloaded;
        public PlayerProfile ActiveProfile { get; private set; }
        public bool IsDirty => ActiveProfile?.IsDirty ?? false;
        public ProfileOperationResult LastLoadResult { get; private set; } = ProfileOperationResult.Ok();
        public ProfileOperationResult LastSaveResult { get; private set; } = ProfileOperationResult.Ok();
        public bool EnableBackups { get; set; } = true;

        public AutosavePolicy AutosavePolicy
        {
            get => _autosavePolicy;
            set => _autosavePolicy = value ?? AutosavePolicy.Default;
        }

        public event Action<ProfileId> ProfileLoading;
        public event Action<PlayerProfile> ProfileLoaded;
        public event Action<ProfileId, string> ProfileLoadFailed;
        public event Action<ProfileId> ProfileSaving;
        public event Action<ProfileId> ProfileSaved;
        public event Action<ProfileId, string> ProfileSaveFailed;
        public event Action<PlayerProfile> ProfileUnloading;
        public event Action<ProfileId> ProfileUnloaded;
        public event Action<ProfileId, ProfileId> ProfileSwitched;

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            _timer = registry.Get<ITimerService>();
            registry.TryGet(out _log);
            registry.TryGet(out _scene);

            _index = _persistence.Load(IndexKey, IndexVersion, new PlayerProfileIndexData());

            if (_scene != null)
            {
                _scene.SceneLoaded += OnSceneTransition;
                _scene.SceneUnloaded += OnSceneTransition;
            }

            var driverObject = new GameObject(nameof(PlayerDataLifecycleDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<PlayerDataLifecycleDriver>();
            _driver.Owner = this;

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            bool autoFlushOnTeardown = (_autosavePolicy.Triggers & AutosaveTriggers.ProfileUnload) != 0;
            if (autoFlushOnTeardown && !_busy && ActiveProfile != null && ActiveProfile.IsDirty)
            {
                try
                {
                    SaveCore();
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                }
            }

            CancelPendingAutosave();

            if (_scene != null)
            {
                _scene.SceneLoaded -= OnSceneTransition;
                _scene.SceneUnloaded -= OnSceneTransition;
                _scene = null;
            }

            if (_driver != null)
            {
                DestroySafely(_driver.gameObject);
                _driver = null;
            }

            ActiveProfile = null;
            State = ProfileState.Unloaded;
            _factoriesByType.Clear();
            _typeById.Clear();
            _sectionIdOrder.Clear();
            _pendingMigrationsBySectionId.Clear();
            _migrationsForwardedForKey.Clear();
        }

        public void RegisterSection<TSection>(Func<TSection> factory) where TSection : class, IPlayerDataSection
        {
            Guard.NotNull(factory, nameof(factory));

            if (ActiveProfile != null)
            {
                throw new InvalidOperationException(
                    $"Cannot register section '{typeof(TSection).Name}' after a profile has been loaded.");
            }

            Type type = typeof(TSection);
            if (_factoriesByType.ContainsKey(type))
            {
                throw new InvalidOperationException($"A section of type '{type.Name}' is already registered.");
            }

            IPlayerDataSection probe = factory();
            if (probe == null)
            {
                throw new ArgumentException($"Factory for '{type.Name}' returned null.", nameof(factory));
            }

            string id = probe.Id;
            Guard.NotNullOrEmpty(id, $"{type.Name}.Id");

            if (_typeById.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Section id '{id}' is already registered by a different section type. Section ids must be unique.");
            }

            _factoriesByType.Add(type, factory);
            _typeById.Add(id, type);
            _sectionIdOrder.Add(id);
        }

        public void RegisterMigration(string sectionId, ISaveMigration migration)
        {
            Guard.NotNullOrEmpty(sectionId, nameof(sectionId));
            Guard.NotNull(migration, nameof(migration));

            if (ActiveProfile != null)
            {
                throw new InvalidOperationException(
                    $"Cannot register a migration for section '{sectionId}' after a profile has been loaded.");
            }

            if (!_pendingMigrationsBySectionId.TryGetValue(sectionId, out List<ISaveMigration> migrations))
            {
                migrations = new List<ISaveMigration>();
                _pendingMigrationsBySectionId.Add(sectionId, migrations);
            }

            migrations.Add(migration);
        }

        public IReadOnlyList<ProfileId> ListProfiles()
        {
            var result = new List<ProfileId>(_index.ProfileIds.Count);
            for (int i = 0; i < _index.ProfileIds.Count; i++)
            {
                result.Add(new ProfileId(_index.ProfileIds[i]));
            }

            return result;
        }

        public bool ProfileExists(ProfileId id) => ContainsInIndex(id);

        public bool TryGetProfileInfo(ProfileId id, out PlayerProfileInfo info)
        {
            if (!ContainsInIndex(id))
            {
                info = default;
                return false;
            }

            PlayerProfileMetadata metadata = _persistence.Load(BuildMetadataKey(id), MetadataVersion, (PlayerProfileMetadata)null);
            if (metadata == null)
            {
                info = default;
                return false;
            }

            info = new PlayerProfileInfo(metadata);
            return true;
        }

        public ProfileOperationResult CreateProfile(ProfileId id)
        {
            ValidateProfileId(id);

            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            _busy = true;
            try
            {
                return CreateCore(id);
            }
            finally
            {
                _busy = false;
            }
        }

        public ProfileOperationResult LoadProfile(ProfileId id)
        {
            ValidateProfileId(id);

            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            if (State != ProfileState.Unloaded)
            {
                return ProfileOperationResult.InvalidState("A profile is already active; unload or switch first.");
            }

            _busy = true;
            try
            {
                return LoadCore(id);
            }
            finally
            {
                _busy = false;
            }
        }

        public ProfileOperationResult LoadDefaultProfile()
        {
            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            if (!ContainsInIndex(ProfileId.Default))
            {
                ProfileOperationResult createResult = CreateProfile(ProfileId.Default);
                if (!createResult.Success)
                {
                    return createResult;
                }
            }

            return LoadProfile(ProfileId.Default);
        }

        public ProfileOperationResult UnloadActiveProfile(bool saveIfDirty = true)
        {
            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            if (State == ProfileState.Unloaded)
            {
                return ProfileOperationResult.Ok();
            }

            if (State != ProfileState.Loaded)
            {
                return ProfileOperationResult.InvalidState($"Cannot unload while {State}.");
            }

            _busy = true;
            try
            {
                return UnloadCore(saveIfDirty);
            }
            finally
            {
                _busy = false;
            }
        }

        public ProfileOperationResult SwitchProfile(ProfileId id, bool saveCurrentIfDirty = true)
        {
            ValidateProfileId(id);

            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            _busy = true;
            try
            {
                PlayerProfile current = ActiveProfile;
                if (current != null && current.Id == id)
                {
                    return ProfileOperationResult.Ok("Already the active profile.");
                }

                ProfileId previousId = current?.Id ?? default;

                if (current != null)
                {
                    UnloadCore(saveCurrentIfDirty);
                }

                ProfileOperationResult loadResult = LoadCore(id);
                if (loadResult.Success || loadResult.Kind == ProfileOperationResultKind.Corrupted)
                {
                    RaiseProfileSwitched(previousId, id);
                }

                return loadResult;
            }
            finally
            {
                _busy = false;
            }
        }

        public ProfileOperationResult DeleteProfile(ProfileId id)
        {
            ValidateProfileId(id);

            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            _busy = true;
            try
            {
                if (ActiveProfile != null && ActiveProfile.Id == id)
                {
                    return ProfileOperationResult.InvalidState("Cannot delete the active profile; unload or switch away from it first.");
                }

                if (!ContainsInIndex(id))
                {
                    return ProfileOperationResult.NotFound($"Profile '{id}' does not exist.");
                }

                DeleteKeyAndCompanions(BuildMetadataKey(id));
                for (int i = 0; i < _sectionIdOrder.Count; i++)
                {
                    DeleteKeyAndCompanions(BuildSectionKey(id, _sectionIdOrder[i]));
                }

                _index.ProfileIds.Remove(id.Value);
                PersistIndex();

                return ProfileOperationResult.Ok();
            }
            finally
            {
                _busy = false;
            }
        }

        public ProfileOperationResult Save()
        {
            if (_busy)
            {
                return ProfileOperationResult.AlreadyActive();
            }

            if (State != ProfileState.Loaded)
            {
                return ProfileOperationResult.InvalidState("No active profile to save.");
            }

            _busy = true;
            State = ProfileState.Saving;
            try
            {
                return SaveCore();
            }
            finally
            {
                _busy = false;
                State = ActiveProfile != null ? ProfileState.Loaded : ProfileState.Unloaded;
            }
        }

        public PlayerProfileDiagnostics GetDiagnostics()
        {
            return new PlayerProfileDiagnostics(
                State,
                ActiveProfile?.Id ?? default,
                ActiveProfile != null,
                IsDirty,
                LastLoadResult,
                LastSaveResult,
                _pendingDebounceTimer != null && _pendingDebounceTimer.IsActive,
                _sectionIdOrder);
        }

        /// <summary>Called by <see cref="PlayerDataLifecycleDriver"/> only - see that class's remarks.</summary>
        internal void HandleApplicationPaused()
        {
            if ((_autosavePolicy.Triggers & AutosaveTriggers.ApplicationPause) != 0)
            {
                TryImmediateAutosave();
            }
        }

        /// <summary>Called by <see cref="PlayerDataLifecycleDriver"/> only - see that class's remarks.</summary>
        internal void HandleFocusLost()
        {
            if ((_autosavePolicy.Triggers & AutosaveTriggers.FocusLost) != 0)
            {
                TryImmediateAutosave();
            }
        }

        /// <summary>Called by <see cref="PlayerDataLifecycleDriver"/> only - see that class's remarks.</summary>
        internal void HandleApplicationQuitting()
        {
            // Best-effort and unconditional - see CLAUDE.md's Phase 13 brief, section 16: never
            // rely exclusively on quit, but still use it as a last supplementary chance.
            TryImmediateAutosave();
        }

        private void OnSceneTransition(string sceneName)
        {
            if ((_autosavePolicy.Triggers & AutosaveTriggers.SceneTransition) != 0)
            {
                TryImmediateAutosave();
            }
        }

        private void TryImmediateAutosave()
        {
            if (_busy || State != ProfileState.Loaded || ActiveProfile == null || !ActiveProfile.IsDirty)
            {
                return;
            }

            _busy = true;
            State = ProfileState.Saving;
            try
            {
                SaveCore();
            }
            finally
            {
                _busy = false;
                State = ActiveProfile != null ? ProfileState.Loaded : ProfileState.Unloaded;
            }
        }

        private void HandleSectionDirty()
        {
            if ((_autosavePolicy.Triggers & AutosaveTriggers.DirtyDebounce) == 0 || ActiveProfile == null)
            {
                return;
            }

            CancelPendingAutosave();
            float delay = Mathf.Max(0f, _autosavePolicy.DebounceSeconds);
            _pendingDebounceTimer = _timer.StartOneShot(delay, OnDebounceElapsed, TimerTimeMode.Unscaled);
        }

        private void OnDebounceElapsed()
        {
            _pendingDebounceTimer = null;
            TryImmediateAutosave();
        }

        private void CancelPendingAutosave()
        {
            _pendingDebounceTimer?.Cancel();
            _pendingDebounceTimer = null;
        }

        private ProfileOperationResult CreateCore(ProfileId id)
        {
            if (ContainsInIndex(id))
            {
                return ProfileOperationResult.AlreadyExists($"Profile '{id}' already exists.");
            }

            Dictionary<Type, IPlayerDataSection> sections = BuildFreshSections();
            PlayerProfileMetadata metadata = PlayerProfileMetadata.CreateNew(id, ProfileSchemaVersion);

            _persistence.Save(BuildMetadataKey(id), metadata, MetadataVersion);

            foreach (KeyValuePair<Type, IPlayerDataSection> pair in sections)
            {
                IPlayerDataSection section = pair.Value;
                ForwardPendingMigrations(id, section.Id);
                section.Save(_persistence, BuildSectionKey(id, section.Id));
            }

            _index.ProfileIds.Add(id.Value);
            PersistIndex();

            return ProfileOperationResult.Ok();
        }

        private ProfileOperationResult LoadCore(ProfileId id)
        {
            State = ProfileState.Loading;
            RaiseProfileLoading(id);

            if (!ContainsInIndex(id))
            {
                var notFound = ProfileOperationResult.NotFound($"Profile '{id}' does not exist.");
                LastLoadResult = notFound;
                State = ProfileState.Unloaded;
                RaiseProfileLoadFailed(id, notFound.Reason);
                return notFound;
            }

            Dictionary<Type, IPlayerDataSection> sections = BuildFreshSections();
            var corruptedDetails = new List<string>();

            string metaKey = BuildMetadataKey(id);
            PlayerProfileMetadata metadata = _persistence.Load(metaKey, MetadataVersion, (PlayerProfileMetadata)null);
            if (metadata == null)
            {
                PlayerProfileMetadata backupMetadata = _persistence.Load(metaKey + BackupSuffix, MetadataVersion, (PlayerProfileMetadata)null);
                if (backupMetadata != null)
                {
                    metadata = backupMetadata;
                    corruptedDetails.Add("metadata (restored from backup)");
                }
                else
                {
                    metadata = PlayerProfileMetadata.CreateNew(id, ProfileSchemaVersion);
                    corruptedDetails.Add("metadata (reset to defaults)");
                }
            }

            foreach (KeyValuePair<Type, IPlayerDataSection> pair in sections)
            {
                IPlayerDataSection section = pair.Value;
                ForwardPendingMigrations(id, section.Id);
                string key = BuildSectionKey(id, section.Id);

                try
                {
                    if (TryLoadSectionWithRecovery(id, section, key, out string detail))
                    {
                        corruptedDetails.Add(detail);
                    }
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                    section.ResetToDefaults();
                    corruptedDetails.Add($"{section.Id} (reset to defaults after validation failure)");
                }
            }

            metadata.LastPlayedAtUtc = DateTime.UtcNow.ToString("O");

            var profile = new PlayerProfile(id, metadata, sections);
            ActiveProfile = profile;
            State = ProfileState.Loaded;

            ProfileOperationResult result = corruptedDetails.Count == 0
                ? ProfileOperationResult.Ok()
                : ProfileOperationResult.Corrupted(string.Join("; ", corruptedDetails));
            LastLoadResult = result;

            RaiseProfileLoaded(profile);
            return result;
        }

        private ProfileOperationResult UnloadCore(bool saveIfDirty)
        {
            PlayerProfile profile = ActiveProfile;
            State = ProfileState.Unloading;
            RaiseProfileUnloading(profile);

            if (saveIfDirty && profile.IsDirty)
            {
                SaveCore();
            }

            CancelPendingAutosave();
            ActiveProfile = null;
            State = ProfileState.Unloaded;
            RaiseProfileUnloaded(profile.Id);
            return ProfileOperationResult.Ok();
        }

        private ProfileOperationResult SaveCore()
        {
            PlayerProfile profile = ActiveProfile;
            if (!profile.IsDirty)
            {
                return ProfileOperationResult.Ok();
            }

            RaiseProfileSaving(profile.Id);

            var failedSectionIds = new List<string>();
            foreach (IPlayerDataSection section in profile.Sections)
            {
                if (!section.IsDirty)
                {
                    continue;
                }

                string key = BuildSectionKey(profile.Id, section.Id);
                try
                {
                    if (EnableBackups)
                    {
                        section.CreateBackup(_persistence, key, key + BackupSuffix);
                    }

                    section.Save(_persistence, key);
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                    failedSectionIds.Add(section.Id);
                }
            }

            profile.Metadata.LastModifiedAtUtc = DateTime.UtcNow.ToString("O");
            try
            {
                _persistence.Save(BuildMetadataKey(profile.Id), profile.Metadata, MetadataVersion);
            }
            catch (Exception exception)
            {
                _log?.LogException(exception, LogCategory);
                failedSectionIds.Add("<metadata>");
            }

            CancelPendingAutosave();

            ProfileOperationResult result = failedSectionIds.Count == 0
                ? ProfileOperationResult.Ok()
                : ProfileOperationResult.Fail($"Section(s) failed to save: {string.Join(", ", failedSectionIds)}");
            LastSaveResult = result;

            if (result.Success)
            {
                RaiseProfileSaved(profile.Id);
            }
            else
            {
                RaiseProfileSaveFailed(profile.Id, result.Reason);
            }

            return result;
        }

        private bool TryLoadSectionWithRecovery(ProfileId profileId, IPlayerDataSection section, string key, out string detail)
        {
            string corruptKey = key + CorruptSuffix;
            if (_persistence.Exists(corruptKey))
            {
                _persistence.Delete(corruptKey);
            }

            section.Load(_persistence, key);

            if (!_persistence.Exists(corruptKey))
            {
                detail = null;
                return false;
            }

            string backupKey = key + BackupSuffix;
            if (_persistence.Exists(backupKey))
            {
                try
                {
                    section.Load(_persistence, backupKey);
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"Section '{section.Id}' for profile '{profileId}' was corrupted; restored from backup.");
                    detail = $"{section.Id} (restored from backup)";
                    return true;
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                }
            }

            _log?.Log(LogLevel.Error, LogCategory,
                $"Section '{section.Id}' for profile '{profileId}' is corrupted and has no usable backup; reset to defaults.");
            section.ResetToDefaults();
            detail = $"{section.Id} (reset to defaults)";
            return true;
        }

        private Dictionary<Type, IPlayerDataSection> BuildFreshSections()
        {
            var result = new Dictionary<Type, IPlayerDataSection>();
            foreach (KeyValuePair<Type, Func<IPlayerDataSection>> pair in _factoriesByType)
            {
                IPlayerDataSection section = pair.Value();
                if (section == null)
                {
                    throw new InvalidOperationException($"Factory for '{pair.Key.Name}' returned null.");
                }

                if (section is IDirtyNotifyingSection notifying)
                {
                    notifying.DirtyNotifier = HandleSectionDirty;
                }

                section.ResetToDefaults();
                result.Add(pair.Key, section);
            }

            return result;
        }

        private void ForwardPendingMigrations(ProfileId id, string sectionId)
        {
            string key = BuildSectionKey(id, sectionId);
            if (!_migrationsForwardedForKey.Add(key))
            {
                return;
            }

            if (_pendingMigrationsBySectionId.TryGetValue(sectionId, out List<ISaveMigration> migrations))
            {
                for (int i = 0; i < migrations.Count; i++)
                {
                    _persistence.RegisterMigration(key, migrations[i]);
                }
            }
        }

        private void DeleteKeyAndCompanions(string key)
        {
            _persistence.Delete(key);
            _persistence.Delete(key + BackupSuffix);
            _persistence.Delete(key + CorruptSuffix);
        }

        private bool ContainsInIndex(ProfileId id) => _index.ProfileIds.Contains(id.Value);

        private void PersistIndex() => _persistence.Save(IndexKey, _index, IndexVersion);

        /// <summary>Internal (not private) purely so tests can pre-seed/inspect a profile's raw
        /// persisted state without duplicating this format - see <c>MigrationTests</c>/
        /// <c>CorruptionAndBackupTests</c>.</summary>
        internal static string BuildSectionKey(ProfileId id, string sectionId) => $"{KeyPrefix}{id.Value}.{sectionId}";

        internal static string BuildMetadataKey(ProfileId id) => $"{KeyPrefix}{id.Value}{MetadataKeySuffix}";

        private static void ValidateProfileId(ProfileId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Profile id must not be null or empty.", nameof(id));
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string value = id.Value;
            for (int i = 0; i < value.Length; i++)
            {
                for (int j = 0; j < invalidChars.Length; j++)
                {
                    if (value[i] == invalidChars[j])
                    {
                        throw new ArgumentException(
                            $"Profile id '{value}' contains a character ('{value[i]}') that is not safe to use in a storage key.",
                            nameof(id));
                    }
                }
            }
        }

        private static void DestroySafely(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }

        private void RaiseProfileLoading(ProfileId id)
        {
            ProfileLoading?.Invoke(id);
            _events.Publish(new ProfileLoadingEvent(id));
        }

        private void RaiseProfileLoaded(PlayerProfile profile)
        {
            ProfileLoaded?.Invoke(profile);
            _events.Publish(new ProfileLoadedEvent(profile.Id));
        }

        private void RaiseProfileLoadFailed(ProfileId id, string reason)
        {
            ProfileLoadFailed?.Invoke(id, reason);
            _events.Publish(new ProfileLoadFailedEvent(id, reason));
        }

        private void RaiseProfileSaving(ProfileId id)
        {
            ProfileSaving?.Invoke(id);
            _events.Publish(new ProfileSavingEvent(id));
        }

        private void RaiseProfileSaved(ProfileId id)
        {
            ProfileSaved?.Invoke(id);
            _events.Publish(new ProfileSavedEvent(id));
        }

        private void RaiseProfileSaveFailed(ProfileId id, string reason)
        {
            ProfileSaveFailed?.Invoke(id, reason);
            _events.Publish(new ProfileSaveFailedEvent(id, reason));
        }

        private void RaiseProfileUnloading(PlayerProfile profile)
        {
            ProfileUnloading?.Invoke(profile);
            _events.Publish(new ProfileUnloadingEvent(profile.Id));
        }

        private void RaiseProfileUnloaded(ProfileId id)
        {
            ProfileUnloaded?.Invoke(id);
            _events.Publish(new ProfileUnloadedEvent(id));
        }

        private void RaiseProfileSwitched(ProfileId previousId, ProfileId currentId)
        {
            ProfileSwitched?.Invoke(previousId, currentId);
            _events.Publish(new ProfileSwitchedEvent(previousId, currentId));
        }
    }
}
