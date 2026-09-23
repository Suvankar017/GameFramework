using GameFramework.DeepLinks;
using GameFramework.Notifications.Integration;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    public class NotificationDeepLinkBridgeTests
    {
        private EventService _events;
        private DeepLinkService _deepLinks;
        private NotificationDeepLinkBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out _events);
            _deepLinks = new DeepLinkService();
            _deepLinks.Initialize(registry);
            _deepLinks.SetReady(true);

            _bridge = new NotificationDeepLinkBridge(_events, _deepLinks);
        }

        [TearDown]
        public void TearDown()
        {
            _bridge.Dispose();
            _deepLinks.Shutdown();
        }

        [Test]
        public void NotificationOpened_WithRoute_ProcessesAsDeepLink()
        {
            var handler = new FakeDeepLinkHandler();
            _deepLinks.RegisterHandler(handler);

            var payload = new NotificationPayload(route: "daily-reward");
            _events.Publish(new NotificationOpenedEvent(new NotificationOpenedInfo(new NotificationId("n1"), payload, wasSimulated: true)));

            Assert.AreEqual(1, handler.HandleCallCount);
            Assert.AreEqual("/daily-reward", handler.HandledLinks[0].Path);
        }

        [Test]
        public void NotificationOpened_WithParameters_EncodesAsQueryString()
        {
            var handler = new FakeDeepLinkHandler();
            _deepLinks.RegisterHandler(handler);

            var payload = new NotificationPayload(route: "shop", parameters: new System.Collections.Generic.Dictionary<string, string> { ["item"] = "sword of truth" });
            _events.Publish(new NotificationOpenedEvent(new NotificationOpenedInfo(new NotificationId("n1"), payload, wasSimulated: true)));

            Assert.AreEqual("sword of truth", handler.HandledLinks[0].QueryParameters["item"]);
        }

        [Test]
        public void NotificationOpened_NoRoute_DoesNothing()
        {
            var handler = new FakeDeepLinkHandler();
            _deepLinks.RegisterHandler(handler);

            _events.Publish(new NotificationOpenedEvent(new NotificationOpenedInfo(new NotificationId("n1"), NotificationPayload.Empty, wasSimulated: true)));

            Assert.AreEqual(0, handler.HandleCallCount);
        }

        [Test]
        public void Dispose_StopsForwardingEvents()
        {
            var handler = new FakeDeepLinkHandler();
            _deepLinks.RegisterHandler(handler);
            _bridge.Dispose();

            var payload = new NotificationPayload(route: "daily-reward");
            _events.Publish(new NotificationOpenedEvent(new NotificationOpenedInfo(new NotificationId("n1"), payload, wasSimulated: true)));

            Assert.AreEqual(0, handler.HandleCallCount);
        }
    }
}
