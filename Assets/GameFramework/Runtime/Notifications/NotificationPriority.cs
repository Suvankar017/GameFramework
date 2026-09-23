namespace GameFramework.Notifications
{
    /// <summary>Abstract priority a provider maps onto its own platform concept (Android notification
    /// priority/importance, iOS interruption level) - see CLAUDE.md's Phase 18 brief, section 7.</summary>
    public enum NotificationPriority
    {
        Low,
        Default,
        High
    }
}
