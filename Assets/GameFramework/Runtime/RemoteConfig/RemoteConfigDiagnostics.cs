using System;

namespace GameFramework.RemoteConfig
{
    /// <summary>Development-time snapshot of service health - see CLAUDE.md's Phase 17 brief, section
    /// 81. Read-only; never used to drive gameplay logic (the same constraint
    /// <c>Performance.GameObjectPool.Statistics</c> already documents for its own diagnostics
    /// surface).</summary>
    public readonly struct RemoteConfigDiagnostics
    {
        public readonly RemoteConfigState State;
        public readonly RemoteConfigEnvironment Environment;
        public readonly int Version;
        public readonly int SchemaVersion;
        public readonly RemoteConfigCacheStatus CacheStatus;
        public readonly DateTime? LastFetchUtc;
        public readonly DateTime? LastActivationUtc;
        public readonly string LastError;
        public readonly int KeyCount;

        public RemoteConfigDiagnostics(
            RemoteConfigState state,
            RemoteConfigEnvironment environment,
            int version,
            int schemaVersion,
            RemoteConfigCacheStatus cacheStatus,
            DateTime? lastFetchUtc,
            DateTime? lastActivationUtc,
            string lastError,
            int keyCount)
        {
            State = state;
            Environment = environment;
            Version = version;
            SchemaVersion = schemaVersion;
            CacheStatus = cacheStatus;
            LastFetchUtc = lastFetchUtc;
            LastActivationUtc = lastActivationUtc;
            LastError = lastError;
            KeyCount = keyCount;
        }
    }
}
