namespace GameFramework.RemoteConfig
{
    /// <summary>See CLAUDE.md's Phase 17 brief, section 29/81.</summary>
    public enum RemoteConfigCacheStatus
    {
        /// <summary>No cache has ever been written, or none was found for the active environment.</summary>
        NoCache,

        Fresh,

        /// <summary>Older than <c>StaleThresholdSeconds</c> but still activated (see
        /// <see cref="RemoteConfigConfiguration.UseStaleCache"/>) - "still usable" per section 29.</summary>
        Stale,

        /// <summary>Older than <c>CacheExpirationSeconds</c> - discarded entirely; defaults are used
        /// instead.</summary>
        Expired,

        /// <summary>Failed to deserialize, or targets an unsupported schema/a different environment -
        /// discarded entirely.</summary>
        Corrupt
    }
}
