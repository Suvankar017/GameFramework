using System;
using System.Collections.Generic;
using GameFramework.Performance.Mobile;
using GameFramework.RemoteConfig.Providers;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using UnityEngine;
using Log = GameFramework.Runtime.Diagnostics.Log;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Default <see cref="IRemoteConfigService"/>, orchestrating one <see cref="IRemoteConfigProvider"/>
    /// - see CLAUDE.md's Phase 17 brief for the full design.
    ///
    /// <b>Precedence (section 6):</b> every candidate snapshot is built by copying every declared
    /// <see cref="RemoteConfigDefinition"/>'s local default, then overlaying (in order) a valid cached
    /// payload and, on top of that, a valid fresh fetch - each layer only ever replaces the keys it
    /// actually supplies, so a key a fetch omits still falls through to cache or, failing that,
    /// default (never simply "missing"). Both the cache load at startup and every <see cref="Fetch"/>
    /// call rebuild from a fresh copy of the defaults, so a value a provider used to send but has since
    /// stopped sending is not left lingering.
    ///
    /// <b>Atomicity (sections 14-15/56/77):</b> a candidate snapshot is fully validated before it ever
    /// becomes <see cref="ActiveSnapshot"/>; a single invalid key rejects the entire candidate and
    /// leaves the previous snapshot (reference-swap, hence atomic from any reader's perspective)
    /// active. <see cref="RemoteConfigSnapshot"/> itself is immutable, so a consumer that captured a
    /// reference to one continues to see a fully consistent view even after a new one is activated
    /// elsewhere.
    /// </summary>
    public sealed class RemoteConfigService : IRemoteConfigService
    {
        private const string LogCategory = "RemoteConfig";
        private const string CacheKey = "GameFramework.RemoteConfig.Cache";
        private const int CacheSaveVersion = 1;

        private readonly RemoteConfigConfiguration _configuration;
        private readonly IRemoteConfigProvider _provider;
        private readonly Dictionary<string, RemoteConfigDefinition> _definitionsByKey = new Dictionary<string, RemoteConfigDefinition>(StringComparer.Ordinal);
        private readonly List<RemoteConfigDefinition> _definitionList = new List<RemoteConfigDefinition>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ITimerService _timer;
        private ILoggingService _log;

        private int _fetchGeneration;
        private bool _isFetchInProgress;
        private bool _providerReady;
        private ITimerHandle _fetchTimeoutHandle;
        private RemoteConfigCacheStatus _cacheStatus = RemoteConfigCacheStatus.NoCache;

        public RemoteConfigState State { get; private set; } = RemoteConfigState.NotInitialized;
        public RemoteConfigEnvironment Environment => _configuration.Environment;
        public RemoteConfigSnapshot ActiveSnapshot { get; private set; }
        public IReadOnlyList<RemoteConfigDefinition> Definitions => _definitionList;
        public TimeSpan ServerTimeOffset { get; private set; } = TimeSpan.Zero;
        public DateTime? LastFetchUtc { get; private set; }
        public DateTime? LastActivationUtc { get; private set; }
        public string LastError { get; private set; }

        public bool IsStale =>
            !LastFetchUtc.HasValue || (DateTime.UtcNow - LastFetchUtc.Value).TotalSeconds > _configuration.StaleThresholdSeconds;

        public event Action<RemoteConfigState> StateChanged;
        public event Action<RemoteConfigSnapshot> ConfigurationActivated;

        public RemoteConfigService(RemoteConfigConfiguration configuration, IRemoteConfigProvider provider)
        {
            _configuration = configuration != null ? configuration : ScriptableObject.CreateInstance<RemoteConfigConfiguration>();
            _provider = provider ?? new NoOpRemoteConfigProvider();

            foreach (RemoteConfigDefinition definition in _configuration.Definitions)
            {
                if (definition != null && definition.IsKeyValid)
                {
                    _definitionsByKey[definition.Key] = definition;
                    _definitionList.Add(definition);
                }
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            _timer = registry.Get<ITimerService>();
            registry.TryGet(out _log);

            SetState(RemoteConfigState.Initializing);

            ActiveSnapshot = RemoteConfigSnapshot.FromDefaults(_definitionList, _configuration.Environment, _configuration.SupportedSchemaVersion);
            LoadCacheIfValid();

            SetState(ActiveSnapshot.Version > 0 ? RemoteConfigState.Active : RemoteConfigState.Ready);

            _events.Subscribe<ApplicationResumedEvent>(OnApplicationResumed);

            InitializeProvider(() =>
            {
                if (_configuration.FetchOnStartup)
                {
                    Fetch();
                }
            });
        }

        public void Shutdown()
        {
            _events.Unsubscribe<ApplicationResumedEvent>(OnApplicationResumed);
            _fetchTimeoutHandle?.Cancel();
            _fetchTimeoutHandle = null;
        }

        public bool GetBool(string key, bool defaultValue = false) => TryGetTyped(key, out bool value) ? value : defaultValue;
        public int GetInt(string key, int defaultValue = 0) => TryGetTyped(key, out int value) ? value : defaultValue;
        public long GetLong(string key, long defaultValue = 0L) => TryGetTyped(key, out long value) ? value : defaultValue;
        public float GetFloat(string key, float defaultValue = 0f) => TryGetTyped(key, out float value) ? value : defaultValue;
        public double GetDouble(string key, double defaultValue = 0d) => TryGetTyped(key, out double value) ? value : defaultValue;
        public string GetString(string key, string defaultValue = "") => TryGetTyped(key, out string value) ? value : defaultValue;

        public bool HasKey(string key) => !string.IsNullOrEmpty(key) && ActiveSnapshot.Values.ContainsKey(key);

        public bool TryGetDefinition(string key, out RemoteConfigDefinition definition) =>
            _definitionsByKey.TryGetValue(key ?? string.Empty, out definition);

        public void Fetch(Action<RemoteConfigFetchResult> onComplete = null)
        {
            if (_isFetchInProgress)
            {
                onComplete?.Invoke(new RemoteConfigFetchResult(RemoteConfigFetchResultKind.AlreadyInProgress, ActiveSnapshot, "A fetch is already in progress."));
                return;
            }

            if (!_providerReady)
            {
                InitializeProvider(() => BeginFetch(onComplete));
                return;
            }

            BeginFetch(onComplete);
        }

        public RemoteConfigDiagnostics GetDiagnostics() => new RemoteConfigDiagnostics(
            State, _configuration.Environment, ActiveSnapshot.Version, ActiveSnapshot.SchemaVersion,
            _cacheStatus, LastFetchUtc, LastActivationUtc, LastError, ActiveSnapshot.Values.Count);

        private void InitializeProvider(Action onReady)
        {
            _provider.Initialize(success =>
            {
                _providerReady = success;
                if (!success)
                {
                    LastError = "Remote config provider failed to initialize.";
                    _log?.Log(LogLevel.Warning, LogCategory, LastError);
                    SetState(RemoteConfigState.Failed);
                    return;
                }

                if (State == RemoteConfigState.Failed)
                {
                    SetState(ActiveSnapshot.Version > 0 ? RemoteConfigState.Active : RemoteConfigState.Ready);
                }

                onReady?.Invoke();
            });
        }

        private void BeginFetch(Action<RemoteConfigFetchResult> onComplete)
        {
            _isFetchInProgress = true;
            int generation = ++_fetchGeneration;

            SetState(RemoteConfigState.Fetching);
            _events.Publish(new ConfigFetchStartedEvent());

            _fetchTimeoutHandle = _timer.StartDelay(
                _configuration.FetchTimeoutSeconds,
                () => HandleFetchTimeout(generation, onComplete),
                TimerTimeMode.Unscaled);

            _provider.Fetch(_configuration.SupportedSchemaVersion, result =>
            {
                if (generation != _fetchGeneration)
                {
                    // A late callback for a fetch this service already gave up on (timed out) - see
                    // IRemoteConfigProvider.Fetch's remarks.
                    return;
                }

                _fetchTimeoutHandle?.Cancel();
                _fetchTimeoutHandle = null;
                HandleProviderResult(result, onComplete);
            });
        }

        private void HandleFetchTimeout(int generation, Action<RemoteConfigFetchResult> onComplete)
        {
            if (generation != _fetchGeneration || !_isFetchInProgress)
            {
                return;
            }

            _fetchGeneration++; // Invalidate the in-flight provider callback, if it ever arrives.
            _isFetchInProgress = false;
            _fetchTimeoutHandle = null;

            LastError = "Fetch timed out.";
            _log?.Log(LogLevel.Warning, LogCategory, LastError);
            SetState(ActiveSnapshot.Version > 0 ? RemoteConfigState.Active : RemoteConfigState.Ready);
            _events.Publish(new ConfigFetchFailedEvent(LastError));

            onComplete?.Invoke(new RemoteConfigFetchResult(RemoteConfigFetchResultKind.TimedOut, ActiveSnapshot, LastError));
        }

        private void HandleProviderResult(RemoteConfigProviderResult result, Action<RemoteConfigFetchResult> onComplete)
        {
            _isFetchInProgress = false;

            if (!result.Success)
            {
                LastError = result.FailureDetail;
                _log?.Log(LogLevel.Warning, LogCategory, $"Fetch failed: {result.FailureDetail}");
                SetState(ActiveSnapshot.Version > 0 ? RemoteConfigState.Active : RemoteConfigState.Ready);
                _events.Publish(new ConfigFetchFailedEvent(result.FailureDetail));
                onComplete?.Invoke(new RemoteConfigFetchResult(RemoteConfigFetchResultKind.Failed, ActiveSnapshot, result.FailureDetail));
                return;
            }

            SetState(RemoteConfigState.Activating);

            Dictionary<string, object> candidate = BuildDefaultsCopy();
            Overlay(candidate, result.Values);

            RemoteConfigValidationResult validation = Validate(result.SchemaVersion, candidate);
            if (!validation.IsValid)
            {
                LastError = validation.FailureDetail;
                _log?.Log(LogLevel.Warning, LogCategory, $"Fetched configuration rejected: {validation.FailureDetail}");
                SetState(ActiveSnapshot.Version > 0 ? RemoteConfigState.Active : RemoteConfigState.Ready);
                _events.Publish(new ConfigRejectedEvent(validation.FailureDetail));
                onComplete?.Invoke(new RemoteConfigFetchResult(RemoteConfigFetchResultKind.Failed, ActiveSnapshot, validation.FailureDetail));
                return;
            }

            DateTime fetchedAtUtc = DateTime.UtcNow;
            if (result.ServerTimeUtc.HasValue)
            {
                ServerTimeOffset = result.ServerTimeUtc.Value - fetchedAtUtc;
            }

            LastError = null;
            Activate(new RemoteConfigSnapshot(result.Version, result.SchemaVersion, _configuration.Environment, fetchedAtUtc, DateTime.UtcNow, candidate));

            if (_configuration.CacheEnabled)
            {
                SaveCache(ActiveSnapshot);
                _cacheStatus = RemoteConfigCacheStatus.Fresh;
            }

            _events.Publish(new ConfigFetchSucceededEvent(ActiveSnapshot));
            onComplete?.Invoke(new RemoteConfigFetchResult(RemoteConfigFetchResultKind.Success, ActiveSnapshot));
        }

        private void Activate(RemoteConfigSnapshot snapshot)
        {
            ActiveSnapshot = snapshot;
            LastFetchUtc = snapshot.FetchedAtUtc;
            LastActivationUtc = snapshot.ActivatedAtUtc;

            SetState(RemoteConfigState.Active);
            _events.Publish(new ConfigActivatedEvent(snapshot));
            ConfigurationActivated?.Invoke(snapshot);
        }

        private void LoadCacheIfValid()
        {
            if (!_configuration.CacheEnabled)
            {
                _cacheStatus = RemoteConfigCacheStatus.NoCache;
                return;
            }

            RemoteConfigCacheData cache = _persistence.Load<RemoteConfigCacheData>(CacheKey, CacheSaveVersion, null);
            if (cache == null || cache.Entries.Count == 0)
            {
                _cacheStatus = RemoteConfigCacheStatus.NoCache;
                return;
            }

            if (!string.Equals(cache.Environment, _configuration.Environment.ToString(), StringComparison.Ordinal))
            {
                // Never let a cache written under a different environment leak into this one - see
                // CLAUDE.md's Phase 17 brief, section 58/59.
                _cacheStatus = RemoteConfigCacheStatus.NoCache;
                return;
            }

            if (cache.SchemaVersion > _configuration.SupportedSchemaVersion)
            {
                _cacheStatus = RemoteConfigCacheStatus.Corrupt;
                _log?.Log(LogLevel.Warning, LogCategory, "Discarding cached configuration: unsupported schema version.");
                return;
            }

            DateTime fetchedAtUtc = new DateTime(cache.FetchedAtUtcTicks, DateTimeKind.Utc);
            double ageSeconds = (DateTime.UtcNow - fetchedAtUtc).TotalSeconds;

            if (_configuration.CacheExpirationSeconds > 0f && ageSeconds > _configuration.CacheExpirationSeconds)
            {
                _cacheStatus = RemoteConfigCacheStatus.Expired;
                return;
            }

            bool isStale = ageSeconds > _configuration.StaleThresholdSeconds;
            if (isStale && !_configuration.UseStaleCache)
            {
                _cacheStatus = RemoteConfigCacheStatus.Stale;
                return;
            }

            Dictionary<string, object> candidate = BuildDefaultsCopy();
            var cachedValues = new Dictionary<string, object>(cache.Entries.Count, StringComparer.Ordinal);
            foreach (RemoteConfigCacheData.Entry entry in cache.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    cachedValues[entry.Key] = entry.Value.BoxedValue;
                }
            }
            Overlay(candidate, cachedValues);

            RemoteConfigValidationResult validation = Validate(cache.SchemaVersion, candidate);
            if (!validation.IsValid)
            {
                _cacheStatus = RemoteConfigCacheStatus.Corrupt;
                _log?.Log(LogLevel.Warning, LogCategory, $"Discarding cached configuration: {validation.FailureDetail}");
                return;
            }

            ActiveSnapshot = new RemoteConfigSnapshot(cache.Version, cache.SchemaVersion, _configuration.Environment, fetchedAtUtc, DateTime.UtcNow, candidate);
            LastFetchUtc = fetchedAtUtc;
            LastActivationUtc = ActiveSnapshot.ActivatedAtUtc;
            _cacheStatus = isStale ? RemoteConfigCacheStatus.Stale : RemoteConfigCacheStatus.Fresh;

            // No ConfigurationActivated/ConfigActivatedEvent here deliberately: this runs during this
            // service's own Initialize, before anything could possibly have subscribed yet. A consumer
            // that needs the initial configuration reads ActiveSnapshot directly (as FeatureFlagService/
            // LiveOpsService both do during their own Initialize); the event exists for changes that
            // happen afterward.
        }

        private void SaveCache(RemoteConfigSnapshot snapshot)
        {
            var data = new RemoteConfigCacheData
            {
                Version = snapshot.Version,
                SchemaVersion = snapshot.SchemaVersion,
                Environment = snapshot.Environment.ToString(),
                FetchedAtUtcTicks = snapshot.FetchedAtUtc.Ticks
            };

            foreach (KeyValuePair<string, object> kvp in snapshot.Values)
            {
                data.Entries.Add(new RemoteConfigCacheData.Entry { Key = kvp.Key, Value = RemoteConfigTypedValue.FromBoxed(kvp.Value) });
            }

            _persistence.Save(CacheKey, data, CacheSaveVersion);
        }

        private RemoteConfigValidationResult Validate(int schemaVersion, IReadOnlyDictionary<string, object> candidateValues)
        {
            if (schemaVersion > _configuration.SupportedSchemaVersion)
            {
                return RemoteConfigValidationResult.Invalid(
                    $"Schema version {schemaVersion} is newer than the supported version {_configuration.SupportedSchemaVersion}.");
            }

            foreach (RemoteConfigDefinition definition in _definitionList)
            {
                if (!candidateValues.TryGetValue(definition.Key, out object value))
                {
                    continue;
                }

                if (!IsTypeMatch(definition.Type, value))
                {
                    return RemoteConfigValidationResult.Invalid(
                        $"Key '{definition.Key}' expected type {definition.Type} but received {value?.GetType().Name ?? "null"}.");
                }

                if (definition.HasRange && TryToDouble(value, out double numeric) && (numeric < definition.MinValue || numeric > definition.MaxValue))
                {
                    return RemoteConfigValidationResult.Invalid(
                        $"Key '{definition.Key}' value {numeric} is outside the allowed range [{definition.MinValue}, {definition.MaxValue}].");
                }
            }

            return RemoteConfigValidationResult.Valid();
        }

        private Dictionary<string, object> BuildDefaultsCopy()
        {
            var values = new Dictionary<string, object>(_definitionList.Count, StringComparer.Ordinal);
            foreach (RemoteConfigDefinition definition in _definitionList)
            {
                values[definition.Key] = definition.DefaultValueBoxed;
            }
            return values;
        }

        private static void Overlay(Dictionary<string, object> target, IReadOnlyDictionary<string, object> overlay)
        {
            if (overlay == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object> kvp in overlay)
            {
                target[kvp.Key] = kvp.Value;
            }
        }

        private bool TryGetTyped<T>(string key, out T value)
        {
            if (!string.IsNullOrEmpty(key) && ActiveSnapshot.Values.TryGetValue(key, out object raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        private static bool IsTypeMatch(RemoteConfigValueType type, object value)
        {
            switch (type)
            {
                case RemoteConfigValueType.Bool: return value is bool;
                case RemoteConfigValueType.Int: return value is int;
                case RemoteConfigValueType.Long: return value is long;
                case RemoteConfigValueType.Float: return value is float;
                case RemoteConfigValueType.Double: return value is double;
                default: return value is string;
            }
        }

        private static bool TryToDouble(object value, out double result)
        {
            switch (value)
            {
                case int i: result = i; return true;
                case long l: result = l; return true;
                case float f: result = f; return true;
                case double d: result = d; return true;
                default: result = 0d; return false;
            }
        }

        private void OnApplicationResumed(ApplicationResumedEvent evt)
        {
            if (_configuration.AutoFetchOnResume && IsStale && !_isFetchInProgress)
            {
                Fetch();
            }
        }

        private void SetState(RemoteConfigState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
