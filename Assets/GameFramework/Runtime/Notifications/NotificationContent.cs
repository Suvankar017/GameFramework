namespace GameFramework.Notifications
{
    /// <summary>Provider-independent display content - see CLAUDE.md's Phase 18 brief, section 7.
    /// Never exposes an Android/iOS SDK object.</summary>
    public readonly struct NotificationContent
    {
        public readonly NotificationText Title;
        public readonly NotificationText Body;
        public readonly NotificationText Subtitle;

        public NotificationContent(NotificationText title, NotificationText body, NotificationText subtitle = default)
        {
            Title = title;
            Body = body;
            Subtitle = subtitle;
        }

        public static NotificationContent FromText(string title, string body, string subtitle = null) =>
            new NotificationContent(NotificationText.FromText(title), NotificationText.FromText(body),
                subtitle != null ? NotificationText.FromText(subtitle) : default);
    }
}
