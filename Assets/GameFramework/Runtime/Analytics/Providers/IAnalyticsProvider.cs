namespace GameFramework.Analytics.Providers
{
    /// <summary>
    /// Provider adapter seam - see CLAUDE.md's Phase 16 brief, section 45. An implementation wraps
    /// exactly one external SDK (Firebase Analytics, GameAnalytics, Unity Analytics, ...); no such
    /// adapter is installed in this project (see <see cref="NoOpAnalyticsProvider"/>'s remarks) - only
    /// this seam plus <see cref="NoOpAnalyticsProvider"/>/<see cref="Mock.MockAnalyticsProvider"/>
    /// exist.
    ///
    /// <see cref="AnalyticsService"/> always calls these from the main thread, already validated/
    /// sanitized (event name, parameter count/key/value limits - see
    /// <see cref="AnalyticsConfiguration"/>) and already consent/enabled-gated - an implementation
    /// does not need to re-check any of that. Every call is wrapped in a try/catch by the caller, so
    /// an implementation is free to let a genuine SDK failure throw rather than swallowing it.
    /// </summary>
    public interface IAnalyticsProvider
    {
        void Initialize();

        void TrackEvent(AnalyticsEvent analyticsEvent);

        /// <summary>Only string/int/long/float/double/bool values reach this - see
        /// <see cref="IAnalyticsService.SetUserProperty"/>'s remarks.</summary>
        void SetUserProperty(string key, object value);

        void SetUserId(string userId);

        /// <summary>Mirrors <see cref="ConsentState"/> collapsed to a provider-level on/off switch -
        /// most SDKs only expose a binary "analytics collection enabled" toggle, not a three-state
        /// one.</summary>
        void SetConsent(bool granted);
    }
}
