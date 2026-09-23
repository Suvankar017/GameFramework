using GameFramework.Notifications.Providers.Mock;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Covers the shipped, game-usable <see cref="MockNotificationProvider"/> directly - see
    /// <c>RemoteConfig.Tests.MockProviderTests</c>'s remarks for why this is separate from
    /// <see cref="NotificationServiceTests"/>, which exercises it only through the service.</summary>
    public class MockNotificationProviderTests
    {
        [Test]
        public void Schedule_ThenCancel_RemovesFromScheduled()
        {
            var provider = new MockNotificationProvider();
            var request = new NotificationRequest(new NotificationId("n1"), NotificationContent.FromText("T", "B"), System.DateTime.UtcNow.AddMinutes(5));

            provider.Schedule(request);
            Assert.AreEqual(1, provider.GetScheduled().Count);

            provider.Cancel(new NotificationId("n1"));
            Assert.AreEqual(0, provider.GetScheduled().Count);
        }

        [Test]
        public void AlwaysUnsupported_ReportsUnsupportedPermissionAndSchedulingFailure()
        {
            var provider = new MockNotificationProvider(MockNotificationSimulationMode.AlwaysUnsupported);

            Assert.AreEqual(NotificationPermissionStatus.Unsupported, provider.GetPermissionStatus());
            NotificationResult result = provider.Schedule(new NotificationRequest(new NotificationId("n1"), NotificationContent.FromText("T", "B"), System.DateTime.UtcNow.AddMinutes(5)));
            Assert.AreEqual(NotificationResultKind.Unsupported, result.Kind);
        }

        [Test]
        public void SimulateOpened_UsesScheduledRequestPayloadByDefault()
        {
            var provider = new MockNotificationProvider();
            var payload = new NotificationPayload(route: "shop");
            provider.Schedule(new NotificationRequest(new NotificationId("n1"), NotificationContent.FromText("T", "B"), System.DateTime.UtcNow.AddMinutes(5), payload: payload));

            NotificationOpenedInfo? info = null;
            provider.NotificationOpened += i => info = i;
            provider.SimulateOpened(new NotificationId("n1"));

            Assert.IsTrue(info.HasValue);
            Assert.AreEqual("shop", info.Value.Payload.Route);
        }

        [Test]
        public void RegisterChannel_InvalidId_IsIgnored()
        {
            var provider = new MockNotificationProvider();
            provider.RegisterChannel(default);

            Assert.IsFalse(provider.IsChannelRegistered(""));
        }

        [Test]
        public void TryGetLaunchNotification_ConsumedOnce()
        {
            var provider = new MockNotificationProvider();
            provider.SimulateColdStartLaunch(new NotificationId("n1"));

            Assert.IsTrue(provider.TryGetLaunchNotification(out NotificationOpenedInfo _));
            Assert.IsFalse(provider.TryGetLaunchNotification(out NotificationOpenedInfo _));
        }
    }
}
