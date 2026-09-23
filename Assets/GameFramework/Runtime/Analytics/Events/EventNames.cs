namespace GameFramework.Analytics
{
    /// <summary>
    /// Common, stable event-name constants covering the taxonomy CLAUDE.md's Phase 16 brief, section
    /// 60 suggests (app_*/session_*/screen_*/level_*/tutorial_*/monetization_* families) plus the
    /// exact names <c>Analytics.Integration</c>'s bridges publish. This is guidance, not a closed set
    /// - a game is free to call <see cref="IAnalyticsService.Track"/> with any event name of its own
    /// (section 8); nothing in the framework requires registering a custom event here first.
    /// </summary>
    public static class EventNames
    {
        public const string SessionStart = "session_start";
        public const string SessionEnd = "session_end";
        public const string ScreenView = "screen_view";

        public const string LevelStarted = "level_started";
        public const string LevelCompleted = "level_completed";
        public const string LevelFailed = "level_failed";
        public const string LevelRestarted = "level_restarted";
        public const string CheckpointReached = "checkpoint_reached";

        public const string TutorialStarted = "tutorial_started";
        public const string TutorialCompleted = "tutorial_completed";
        public const string TutorialSkipped = "tutorial_skipped";
        public const string TutorialCancelled = "tutorial_cancelled";

        public const string AdLoaded = "ad_loaded";
        public const string AdShown = "ad_shown";
        public const string AdFailed = "ad_failed";
        public const string AdClosed = "ad_closed";
        public const string AdRewardEarned = "ad_reward_earned";

        public const string PurchaseCompleted = "purchase_completed";
        public const string PurchaseFailed = "purchase_failed";
        public const string RestoreCompleted = "restore_completed";
        public const string EntitlementChanged = "entitlement_changed";

        public const string ConfigFetchStarted = "configuration_fetch_started";
        public const string ConfigFetchSucceeded = "configuration_fetch_succeeded";
        public const string ConfigFetchFailed = "configuration_fetch_failed";
        public const string ConfigActivated = "configuration_activated";
        public const string ConfigRejected = "configuration_rejected";
        public const string FeatureFlagChanged = "feature_flag_evaluated";
        public const string LiveEventStarted = "live_event_started";
        public const string LiveEventEnded = "live_event_completed";

        public const string NotificationScheduled = "notification_scheduled";
        public const string NotificationCancelled = "notification_cancelled";
        public const string NotificationOpened = "notification_opened";
        public const string NotificationReceived = "notification_received";
        public const string NotificationPermissionRequested = "notification_permission_requested";
        public const string NotificationPermissionChanged = "notification_permission_changed";
        public const string DeepLinkReceived = "deep_link_received";
        public const string DeepLinkHandled = "deep_link_handled";
        public const string DeepLinkRejected = "deep_link_rejected";
    }
}
