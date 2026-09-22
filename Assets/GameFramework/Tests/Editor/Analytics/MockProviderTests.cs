using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics.Mock;
using GameFramework.Analytics.Providers.Mock;
using NUnit.Framework;

namespace GameFramework.Analytics.Tests
{
    /// <summary>Covers the shipped <see cref="MockAnalyticsProvider"/>/<see cref="MockCrashReportingProvider"/>
    /// in their own right - see <c>Monetization.Tests.MockProviderTests</c>'s remarks for why this is
    /// kept separate from the Fake-based service tests.</summary>
    public class MockProviderTests
    {
        [Test]
        public void MockAnalyticsProvider_RecordsEverything()
        {
            var provider = new MockAnalyticsProvider();

            provider.Initialize();
            provider.SetUserId("user-1");
            provider.SetConsent(true);
            provider.SetUserProperty("color", "blue");
            provider.TrackEvent(new AnalyticsEvent("custom_event", new Dictionary<string, object> { ["a"] = 1 }, System.DateTime.UtcNow));

            Assert.IsTrue(provider.IsInitialized);
            Assert.AreEqual("user-1", provider.UserId);
            Assert.IsTrue(provider.ConsentGranted);
            Assert.AreEqual("blue", provider.UserProperties["color"]);
            Assert.AreEqual(1, provider.TrackedEvents.Count);
            Assert.AreEqual("custom_event", provider.TrackedEvents[0].Name);
        }

        [Test]
        public void MockCrashReportingProvider_RecordsEverything()
        {
            var provider = new MockCrashReportingProvider();

            provider.Initialize();
            provider.SetUserId("user-1");
            provider.Report(new Analytics.Diagnostics.DiagnosticReport(
                null, "test error", Analytics.Diagnostics.ErrorCategory.Unknown, System.DateTime.UtcNow, null, null, null));

            Assert.IsTrue(provider.IsInitialized);
            Assert.AreEqual("user-1", provider.UserId);
            Assert.AreEqual(1, provider.ReceivedReports.Count);
            Assert.AreEqual("test error", provider.ReceivedReports[0].Message);
        }
    }
}
