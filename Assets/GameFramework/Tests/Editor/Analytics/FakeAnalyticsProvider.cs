using System;
using System.Collections.Generic;
using GameFramework.Analytics.Providers;

namespace GameFramework.Analytics.Tests
{
    /// <summary>Fully-controllable test double, distinct from the shipped
    /// <see cref="Providers.Mock.MockAnalyticsProvider"/> (covered in its own right by
    /// <c>MockProviderTests</c>) - see <c>Monetization.Tests.FakeAdProvider</c>'s remarks for why the
    /// two roles are kept separate.</summary>
    internal sealed class FakeAnalyticsProvider : IAnalyticsProvider
    {
        public readonly List<AnalyticsEvent> TrackedEvents = new List<AnalyticsEvent>();
        public readonly Dictionary<string, object> UserProperties = new Dictionary<string, object>();
        public string UserId;
        public bool ConsentGranted;
        public int InitializeCallCount;
        public bool ThrowOnTrackEvent;

        public void Initialize() => InitializeCallCount++;

        public void TrackEvent(AnalyticsEvent analyticsEvent)
        {
            if (ThrowOnTrackEvent)
            {
                throw new InvalidOperationException("Simulated provider failure.");
            }

            TrackedEvents.Add(analyticsEvent);
        }

        public void SetUserProperty(string key, object value) => UserProperties[key] = value;
        public void SetUserId(string userId) => UserId = userId;
        public void SetConsent(bool granted) => ConsentGranted = granted;
    }
}
