namespace GameFramework.Analytics
{
    /// <summary>
    /// Provider-independent consent state - see CLAUDE.md's Phase 16 brief, section 35. This
    /// framework never builds a consent UI; a game's own UI/legal layer decides when to call
    /// <see cref="IAnalyticsService.SetConsent"/>, this only defines the states and what the service
    /// does in response to each one.
    /// </summary>
    public enum ConsentState
    {
        /// <summary>No consent decision has been made yet. Behavior while in this state is governed
        /// by <see cref="ConsentPolicy"/>.</summary>
        Unknown,

        /// <summary>The player has explicitly granted analytics consent.</summary>
        Granted,

        /// <summary>The player has explicitly denied analytics consent. Events are dropped, never
        /// buffered, for as long as this is current.</summary>
        Denied
    }
}
