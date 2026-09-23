using System;
using System.Collections.Generic;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Player-profile and player-data orchestration on top of Phase 2's
    /// <see cref="Runtime.Persistence.IPersistenceService"/> - see CLAUDE.md's Phase 13 brief. This
    /// service owns profile identity/lifecycle/switching, section registration, dirty tracking, and
    /// autosave scheduling; it never touches a file/serializer directly - every byte still goes
    /// through <see cref="IPersistenceService"/>.
    ///
    /// Every command returns a <see cref="ProfileOperationResult"/> instead of throwing - the same
    /// non-throwing pattern <c>GameFlow.IGameFlowService</c>/<c>UI.Navigation.INavigationService</c>
    /// already established, so normal flow (a duplicate load request, deleting a profile that
    /// doesn't exist) never needs a try/catch. Every command issued while another is already in
    /// progress (<see cref="State"/> is not stable) is rejected with
    /// <see cref="ProfileOperationResultKind.AlreadyActive"/>, including one issued synchronously
    /// from inside this service's own event handlers - defer such a follow-up call by one frame
    /// (a coroutine, or a <see cref="Runtime.Timers.ITimerService"/> one-shot) instead of calling
    /// back in directly, the same re-entrancy rule <c>UI.Navigation.INavigationService</c> documents.
    /// </summary>
    public interface IPlayerProfileService : IGameService
    {
        ProfileState State { get; }

        /// <summary>The active profile, or null while <see cref="ProfileState.Unloaded"/>.</summary>
        PlayerProfile ActiveProfile { get; }

        /// <summary>Shortcut for <c>ActiveProfile?.IsDirty ?? false</c>.</summary>
        bool IsDirty { get; }

        ProfileOperationResult LastLoadResult { get; }
        ProfileOperationResult LastSaveResult { get; }

        /// <summary>Every recovery performed during the most recent profile load (empty for a clean
        /// load) - the structured counterpart to a <see cref="ProfileOperationResultKind.Corrupted"/>
        /// result's <see cref="ProfileOperationResult.Reason"/>. See <see cref="SectionRecovery"/>.</summary>
        IReadOnlyList<SectionRecovery> LastLoadRecoveries { get; }

        /// <summary>Governs automatic saving - see <see cref="AutosavePolicy"/>. Assigning a new
        /// policy takes effect on the next dirty-marking mutation/trigger; it does not retroactively
        /// cancel or reschedule an autosave already pending.</summary>
        AutosavePolicy AutosavePolicy { get; set; }

        /// <summary>When true (the default), saving a section that already has data on disk first
        /// copies that on-disk data to a "<c>.bak</c>" companion key before overwriting it - see
        /// CLAUDE.md's Phase 13 brief, section 27. Costs one extra load+save per dirty section per
        /// save cycle (never per-mutation), so a game that cannot afford that can disable it.</summary>
        bool EnableBackups { get; set; }

        /// <summary>
        /// Registers a factory for one section type, unique per <typeparamref name="TSection"/> -
        /// see CLAUDE.md's Phase 13 brief, section 10. Must be called before any profile is loaded;
        /// registering the same section type twice, or registering after a profile is already
        /// active, throws <see cref="InvalidOperationException"/> - this is a composition-root
        /// mistake, not a recoverable runtime condition, the same reasoning
        /// <c>Runtime.Persistence.IPersistenceService.RegisterMigration</c> already applies to a
        /// duplicate migration registration.
        /// </summary>
        void RegisterSection<TSection>(Func<TSection> factory) where TSection : class, IPlayerDataSection;

        /// <summary>Registers one version-chain migration step for the section whose
        /// <see cref="IPlayerDataSection.Id"/> is <paramref name="sectionId"/> - forwarded to
        /// <see cref="IPersistenceService.RegisterMigration"/> against each profile's concrete
        /// storage key for that section as each profile is loaded. Must be called before any profile
        /// using that section is loaded.</summary>
        void RegisterMigration(string sectionId, ISaveMigration migration);

        IReadOnlyList<ProfileId> ListProfiles();

        bool ProfileExists(ProfileId id);

        /// <summary>Returns the persisted metadata for <paramref name="id"/> without loading it as
        /// the active profile - for a save-slot UI the game builds itself.</summary>
        bool TryGetProfileInfo(ProfileId id, out PlayerProfileInfo info);

        /// <summary>Creates a new profile with every registered section at its defaults and
        /// persists it immediately. Fails with <see cref="ProfileOperationResultKind.AlreadyExists"/>
        /// if <paramref name="id"/> already exists.</summary>
        ProfileOperationResult CreateProfile(ProfileId id);

        /// <summary>Loads <paramref name="id"/> as the active profile. Fails with
        /// <see cref="ProfileOperationResultKind.InvalidState"/> if a profile is already active -
        /// call <see cref="UnloadActiveProfile"/> or <see cref="SwitchProfile"/> first.</summary>
        ProfileOperationResult LoadProfile(ProfileId id);

        /// <summary>Convenience for a game that never exposes profile selection - see
        /// CLAUDE.md's Phase 13 brief, section 35. Creates <see cref="ProfileId.Default"/> if it
        /// does not exist yet, then loads it.</summary>
        ProfileOperationResult LoadDefaultProfile();

        /// <summary>Unloads the active profile, saving first if <paramref name="saveIfDirty"/> and
        /// it is dirty. A no-op success if no profile is active.</summary>
        ProfileOperationResult UnloadActiveProfile(bool saveIfDirty = true);

        /// <summary>Safely moves from the current active profile (if any) to <paramref name="id"/> -
        /// see CLAUDE.md's Phase 13 brief, section 33. Equivalent to
        /// <see cref="UnloadActiveProfile"/> followed by <see cref="LoadProfile"/>, as one guarded
        /// operation.</summary>
        ProfileOperationResult SwitchProfile(ProfileId id, bool saveCurrentIfDirty = true);

        /// <summary>Permanently deletes a profile's data. Fails with
        /// <see cref="ProfileOperationResultKind.InvalidState"/> if <paramref name="id"/> is the
        /// currently active profile - unload or switch away from it first, so deletion never races
        /// an in-memory profile still being used.</summary>
        ProfileOperationResult DeleteProfile(ProfileId id);

        /// <summary>Saves the active profile's dirty sections now, bypassing any pending debounce
        /// timer. Fails with <see cref="ProfileOperationResultKind.InvalidState"/> if no profile is
        /// active. Succeeds as a no-op if the active profile is not dirty.</summary>
        ProfileOperationResult Save();

        PlayerProfileDiagnostics GetDiagnostics();

        event Action<ProfileId> ProfileLoading;
        event Action<PlayerProfile> ProfileLoaded;
        event Action<ProfileId, string> ProfileLoadFailed;
        event Action<ProfileId> ProfileSaving;
        event Action<ProfileId> ProfileSaved;
        event Action<ProfileId, string> ProfileSaveFailed;
        event Action<PlayerProfile> ProfileUnloading;
        event Action<ProfileId> ProfileUnloaded;
        event Action<ProfileId, ProfileId> ProfileSwitched;
    }
}
