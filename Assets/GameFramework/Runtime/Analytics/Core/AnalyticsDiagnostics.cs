namespace GameFramework.Analytics
{
    /// <summary>Read-only development/diagnostic snapshot of <see cref="AnalyticsService"/> - the
    /// same "diagnostics struct for a menu-item log or editor window" shape
    /// <c>Monetization.Ads.AdsDiagnostics</c>/<c>Purchases.PurchaseDiagnostics</c> already use. Never
    /// intended to drive gameplay logic.</summary>
    public readonly struct AnalyticsDiagnostics
    {
        public readonly bool IsEnabled;
        public readonly ConsentState Consent;
        public readonly string ProviderName;
        public readonly string SessionId;
        public readonly string UserId;
        public readonly int EventsSentCount;
        public readonly int QueuedEventCount;
        public readonly string LastEventName;

        public AnalyticsDiagnostics(
            bool isEnabled,
            ConsentState consent,
            string providerName,
            string sessionId,
            string userId,
            int eventsSentCount,
            int queuedEventCount,
            string lastEventName)
        {
            IsEnabled = isEnabled;
            Consent = consent;
            ProviderName = providerName;
            SessionId = sessionId;
            UserId = userId;
            EventsSentCount = eventsSentCount;
            QueuedEventCount = queuedEventCount;
            LastEventName = lastEventName;
        }
    }
}
