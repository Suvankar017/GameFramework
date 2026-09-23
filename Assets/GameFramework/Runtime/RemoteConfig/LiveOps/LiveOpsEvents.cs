namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Published by <see cref="LiveOpsService"/> through the Phase 2 Event System, mirroring
    /// its own C# events - see CLAUDE.md's Phase 17 brief, section 82.</summary>
    public readonly struct LiveEventStartedEvent
    {
        public readonly LiveEventId Id;
        public LiveEventStartedEvent(LiveEventId id) => Id = id;
    }

    public readonly struct LiveEventEndedEvent
    {
        public readonly LiveEventId Id;
        public LiveEventEndedEvent(LiveEventId id) => Id = id;
    }
}
