namespace GameFramework.Notifications
{
    /// <summary>
    /// A stable, game-authored notification channel/category - see CLAUDE.md's Phase 18 brief,
    /// section 6/14. Registered once via <see cref="INotificationService.RegisterChannel"/>; a
    /// provider without a channel concept (iOS, the shipped NoOp/Mock providers) simply ignores it -
    /// the framework never assumes every platform needs one.
    /// </summary>
    public readonly struct NotificationChannelDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly NotificationChannelImportance Importance;

        public NotificationChannelDefinition(string id, string displayName, NotificationChannelImportance importance = NotificationChannelImportance.Default)
        {
            Id = id;
            DisplayName = displayName;
            Importance = importance;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);
    }
}
