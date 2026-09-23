namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Published by <see cref="RemoteConfigService"/> through the Phase 2 Event System, mirroring its
    /// own C# events - see CLAUDE.md's Phase 17 brief, section 82: Phase 16 Analytics (via an optional
    /// <c>Analytics.Integration</c> bridge) and a game's own UI/diagnostics layers are the intended
    /// listeners. This framework never calls an analytics/diagnostics provider directly (section 52).
    /// </summary>
    public readonly struct ConfigFetchStartedEvent
    {
    }

    public readonly struct ConfigFetchSucceededEvent
    {
        public readonly RemoteConfigSnapshot Snapshot;
        public ConfigFetchSucceededEvent(RemoteConfigSnapshot snapshot) => Snapshot = snapshot;
    }

    public readonly struct ConfigFetchFailedEvent
    {
        public readonly string FailureDetail;
        public ConfigFetchFailedEvent(string failureDetail) => FailureDetail = failureDetail;
    }

    /// <summary>Published whenever a new snapshot becomes the active one - whether it originated from
    /// a fresh remote fetch or from activating a valid cached payload at startup.</summary>
    public readonly struct ConfigActivatedEvent
    {
        public readonly RemoteConfigSnapshot Snapshot;
        public ConfigActivatedEvent(RemoteConfigSnapshot snapshot) => Snapshot = snapshot;
    }

    /// <summary>Published when a fetched/cached payload failed validation and was therefore never
    /// activated - see CLAUDE.md's Phase 17 brief, section 56.</summary>
    public readonly struct ConfigRejectedEvent
    {
        public readonly string FailureDetail;
        public ConfigRejectedEvent(string failureDetail) => FailureDetail = failureDetail;
    }
}
