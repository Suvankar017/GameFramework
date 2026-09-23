namespace GameFramework.Notifications
{
    /// <summary>Published by <see cref="NotificationService"/> through the Phase 2 Event System,
    /// mirroring its own C# events - see CLAUDE.md's Phase 18 brief, section 29. Phase 16 Analytics
    /// (via the optional <c>Analytics.Integration.NotificationsAnalyticsIntegration</c> bridge) is the
    /// intended listener; this framework never calls an analytics provider directly.</summary>
    public readonly struct NotificationScheduledEvent
    {
        public readonly NotificationId Id;
        public NotificationScheduledEvent(NotificationId id) => Id = id;
    }

    public readonly struct NotificationCancelledEvent
    {
        public readonly NotificationId Id;
        public NotificationCancelledEvent(NotificationId id) => Id = id;
    }

    /// <summary>Raised when a notification is opened (tapped) by the user - see
    /// <see cref="NotificationOpenedInfo"/>.</summary>
    public readonly struct NotificationOpenedEvent
    {
        public readonly NotificationOpenedInfo Info;
        public NotificationOpenedEvent(NotificationOpenedInfo info) => Info = info;
    }

    /// <summary>Raised when a notification is delivered while the app is already running (hot state)
    /// - distinct from <see cref="NotificationOpenedEvent"/>, which implies the user actually tapped
    /// it.</summary>
    public readonly struct NotificationReceivedEvent
    {
        public readonly NotificationOpenedInfo Info;
        public NotificationReceivedEvent(NotificationOpenedInfo info) => Info = info;
    }

    public readonly struct NotificationPermissionRequestedEvent
    {
    }

    public readonly struct NotificationPermissionChangedEvent
    {
        public readonly NotificationPermissionStatus Status;
        public NotificationPermissionChangedEvent(NotificationPermissionStatus status) => Status = status;
    }
}
