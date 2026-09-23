namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Development-time snapshot of Live Ops state - see CLAUDE.md's Phase 17 brief, section
    /// 81. Read-only; never used to drive gameplay logic.</summary>
    public readonly struct LiveOpsDiagnostics
    {
        public readonly int TotalEvents;
        public readonly int ActiveCount;
        public readonly int UpcomingCount;

        public LiveOpsDiagnostics(int totalEvents, int activeCount, int upcomingCount)
        {
            TotalEvents = totalEvents;
            ActiveCount = activeCount;
            UpcomingCount = upcomingCount;
        }
    }
}
