namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Explicit fetch/activation lifecycle - see CLAUDE.md's Phase 17 brief, section 24 (one state
    /// model instead of several unrelated booleans, the same reasoning
    /// <see cref="GameFramework.Monetization.MonetizationProviderState"/> already established for
    /// Phase 15).
    ///
    /// <see cref="Ready"/> and <see cref="Active"/> are distinguished purely by whether the current
    /// <see cref="RemoteConfigSnapshot.Version"/> is 0 (defaults only) or greater (at least one
    /// successful cache/remote activation has occurred) - there is no separately tracked "prior idle
    /// state" to restore after a failed fetch; the service simply re-derives Ready/Active from the
    /// snapshot that remains active. The brief's suggested "Fetched"/"Unavailable" states are
    /// deliberately collapsed: a fetch that completes successfully flows straight from
    /// <see cref="Fetching"/> into <see cref="Activating"/> in the same call, and a provider-reported
    /// "unavailable" outcome is surfaced per-attempt via <see cref="RemoteConfigFetchResultKind"/>
    /// rather than as a lingering service-wide state.
    /// </summary>
    public enum RemoteConfigState
    {
        NotInitialized,
        Initializing,

        /// <summary>Idle; only local default values (and possibly a never-yet-fetched provider) are
        /// in effect.</summary>
        Ready,

        Fetching,

        /// <summary>Transient: a fetch/cache result passed validation and is being swapped in as the
        /// new active snapshot.</summary>
        Activating,

        /// <summary>Idle; at least one successful cache or remote activation has occurred.</summary>
        Active,

        /// <summary>The provider itself failed to initialize - see <see cref="IRemoteConfigProvider.Initialize"/>.
        /// A subsequent <see cref="IRemoteConfigService.Fetch"/> call retries provider initialization
        /// before attempting to fetch.</summary>
        Failed
    }
}
