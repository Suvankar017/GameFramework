namespace GameFramework.RemoteConfig
{
    /// <summary>Outcome of one <see cref="IRemoteConfigService.Fetch"/> call - see CLAUDE.md's Phase
    /// 17 brief, section 55 (every failure path is reported, never thrown).</summary>
    public readonly struct RemoteConfigFetchResult
    {
        public readonly RemoteConfigFetchResultKind Kind;

        /// <summary>The configuration in effect immediately after this call returns - the newly
        /// activated snapshot on <see cref="RemoteConfigFetchResultKind.Success"/>, or whatever was
        /// already active beforehand on any failure path (see CLAUDE.md's Phase 17 brief, section 14:
        /// an invalid/failed fetch never replaces a good snapshot).</summary>
        public readonly RemoteConfigSnapshot Snapshot;

        public readonly string FailureDetail;

        public bool Success => Kind == RemoteConfigFetchResultKind.Success;

        public RemoteConfigFetchResult(RemoteConfigFetchResultKind kind, RemoteConfigSnapshot snapshot, string failureDetail = null)
        {
            Kind = kind;
            Snapshot = snapshot;
            FailureDetail = failureDetail;
        }
    }
}
