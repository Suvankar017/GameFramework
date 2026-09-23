namespace GameFramework.Notifications
{
    /// <summary>Abstract importance a provider maps onto its own platform concept (Android channel
    /// importance; ignored on platforms/providers with no channel concept, e.g. iOS) - see
    /// CLAUDE.md's Phase 18 brief, section 14.</summary>
    public enum NotificationChannelImportance
    {
        Low,
        Default,
        High
    }
}
