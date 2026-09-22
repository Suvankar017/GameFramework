namespace GameFramework.Analytics
{
    /// <summary>Governs what <see cref="AnalyticsService"/> does with a <see cref="IAnalyticsService.Track"/>
    /// call while <see cref="ConsentState"/> is <see cref="ConsentState.Unknown"/> - see CLAUDE.md's
    /// Phase 16 brief, section 37.</summary>
    public enum ConsentPolicy
    {
        /// <summary>Events are held in a bounded, in-memory queue and sent once consent becomes
        /// <see cref="ConsentState.Granted"/>. Cleared, never sent, if consent instead becomes
        /// <see cref="ConsentState.Denied"/> - privacy takes precedence over completeness.</summary>
        BufferUntilDecided,

        /// <summary>Events are dropped outright while consent is undecided - nothing is buffered.</summary>
        DropUntilGranted
    }
}
