namespace GameFramework.RemoteConfig
{
    public enum RemoteConfigFetchResultKind
    {
        Success,

        /// <summary>Covers provider failure, provider-reported unavailability, and validation
        /// rejection alike - see <see cref="RemoteConfigFetchResult.FailureDetail"/> for which.</summary>
        Failed,

        TimedOut,

        /// <summary>A fetch was already in progress - see CLAUDE.md's Phase 17 brief's own
        /// re-entrancy precedent (<c>INavigationService</c>'s <c>AlreadyActive</c>,
        /// <c>IPlayerProfileService</c>'s <c>AlreadyActive</c>): a concurrent/duplicate request is
        /// normal flow, not an exceptional one.</summary>
        AlreadyInProgress
    }
}
