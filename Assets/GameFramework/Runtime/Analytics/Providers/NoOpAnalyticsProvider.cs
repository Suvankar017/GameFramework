namespace GameFramework.Analytics.Providers
{
    /// <summary>
    /// Does nothing - the default provider when no analytics SDK is installed (see CLAUDE.md's
    /// Phase 16 brief, section 48). This is what keeps <see cref="AnalyticsService"/> fully
    /// functional (session/consent/identity bookkeeping, event validation, local diagnostics) even
    /// with zero external SDK present - mandatory for this framework's reusable-project
    /// architecture.
    /// </summary>
    public sealed class NoOpAnalyticsProvider : IAnalyticsProvider
    {
        public void Initialize()
        {
        }

        public void TrackEvent(AnalyticsEvent analyticsEvent)
        {
        }

        public void SetUserProperty(string key, object value)
        {
        }

        public void SetUserId(string userId)
        {
        }

        public void SetConsent(bool granted)
        {
        }
    }
}
