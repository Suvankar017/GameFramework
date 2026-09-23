using System;
using UnityEngine;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Reusable, game-authored Remote Config schema/policy - see CLAUDE.md's Phase 17 brief, sections
    /// 6/9/20/25-29/58. Every limit/threshold here is enforced by <see cref="RemoteConfigService"/>
    /// before a fetched or cached value is ever activated.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/RemoteConfig/Remote Config Configuration", fileName = "RemoteConfigConfiguration")]
    public sealed class RemoteConfigConfiguration : ScriptableObject
    {
        [SerializeField] private RemoteConfigDefinition[] _definitions = Array.Empty<RemoteConfigDefinition>();
        [SerializeField] private RemoteConfigEnvironment _environment = RemoteConfigEnvironment.Development;

        [Tooltip("A fetched/cached payload targeting a schema newer than this is rejected outright (see CLAUDE.md's Phase 17 brief, section 17).")]
        [SerializeField] private int _supportedSchemaVersion = 1;

        [Tooltip("Fetch remote configuration once automatically during Initialize. When false, the game decides when to call Fetch() (section 27) - Ready is still reached immediately either way, using defaults/cache.")]
        [SerializeField] private bool _fetchOnStartup;

        [Tooltip("On app resume, fetch again if the active configuration is currently stale (section 85).")]
        [SerializeField] private bool _autoFetchOnResume;

        [Tooltip("A fetch that has not completed within this many seconds is treated as TimedOut and the existing configuration is kept (section 26).")]
        [SerializeField] private float _fetchTimeoutSeconds = 10f;

        [Tooltip("Seconds after which the active configuration is considered stale - still usable (see UseStaleCache) but eligible for an auto-refetch on resume.")]
        [SerializeField] private float _staleThresholdSeconds = 3600f;

        [Tooltip("Seconds after which a cached configuration is discarded entirely at startup, falling back to defaults (0 = never expires).")]
        [SerializeField] private float _cacheExpirationSeconds = 604800f;

        [Tooltip("Whether a stale-but-not-expired cached configuration may still be activated at startup (section 29).")]
        [SerializeField] private bool _useStaleCache = true;

        [SerializeField] private bool _cacheEnabled = true;

        public RemoteConfigDefinition[] Definitions => _definitions;
        public RemoteConfigEnvironment Environment => _environment;
        public int SupportedSchemaVersion => _supportedSchemaVersion;
        public bool FetchOnStartup => _fetchOnStartup;
        public bool AutoFetchOnResume => _autoFetchOnResume;
        public float FetchTimeoutSeconds => _fetchTimeoutSeconds;
        public float StaleThresholdSeconds => _staleThresholdSeconds;
        public float CacheExpirationSeconds => _cacheExpirationSeconds;
        public bool UseStaleCache => _useStaleCache;
        public bool CacheEnabled => _cacheEnabled;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_supportedSchemaVersion < 1) _supportedSchemaVersion = 1;
            if (_fetchTimeoutSeconds < 0.1f) _fetchTimeoutSeconds = 0.1f;
            if (_staleThresholdSeconds < 0f) _staleThresholdSeconds = 0f;
            if (_cacheExpirationSeconds < 0f) _cacheExpirationSeconds = 0f;

            foreach (RemoteConfigDefinition definition in _definitions)
            {
                definition?.OnValidate();
            }
        }
#endif
    }
}
