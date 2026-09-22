namespace GameFramework.Analytics
{
    /// <summary>Published by <see cref="AnalyticsService"/> through the Phase 2 Event System - no
    /// separate notification mechanism, matching every other cross-system notification in the
    /// framework. A game's own privacy UI is the intended listener (e.g. to update a "manage consent"
    /// screen after <see cref="IAnalyticsService.SetConsent"/> is called from elsewhere).</summary>
    public readonly struct ConsentChangedEvent
    {
        public readonly ConsentState Previous;
        public readonly ConsentState Current;

        public ConsentChangedEvent(ConsentState previous, ConsentState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
