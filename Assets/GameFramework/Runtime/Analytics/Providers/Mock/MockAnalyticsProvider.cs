using System.Collections.Generic;

namespace GameFramework.Analytics.Providers.Mock
{
    /// <summary>
    /// Deterministic, Editor/test-safe provider that records everything it receives in memory - see
    /// CLAUDE.md's Phase 16 brief, section 49. Never sends anything externally. Distinct from the
    /// <c>Tests</c> assembly's own fully-controllable fakes (see that assembly's remarks) - this one
    /// is the shipped, game-usable "I have no real analytics SDK yet but want to see events flow"
    /// provider (e.g. wired up by <see cref="AnalyticsBootstrapper"/>'s inspector toggle), covered by
    /// its own <c>MockProviderTests</c>.
    /// </summary>
    public sealed class MockAnalyticsProvider : IAnalyticsProvider
    {
        public List<AnalyticsEvent> TrackedEvents { get; } = new List<AnalyticsEvent>();
        public Dictionary<string, object> UserProperties { get; } = new Dictionary<string, object>();
        public string UserId { get; private set; }
        public bool ConsentGranted { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void TrackEvent(AnalyticsEvent analyticsEvent)
        {
            TrackedEvents.Add(analyticsEvent);
        }

        public void SetUserProperty(string key, object value)
        {
            UserProperties[key] = value;
        }

        public void SetUserId(string userId)
        {
            UserId = userId;
        }

        public void SetConsent(bool granted)
        {
            ConsentGranted = granted;
        }
    }
}
