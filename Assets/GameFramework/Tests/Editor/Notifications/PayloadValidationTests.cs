using System;
using System.Collections.Generic;
using GameFramework.DeepLinks;
using GameFramework.Notifications.Integration;
using GameFramework.Notifications.Providers.Mock;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Phase 19: inbound notification payloads are untrusted - see
    /// <see cref="NotificationPayloadValidator"/>.</summary>
    public class PayloadValidationTests
    {
        [TestCase("")]
        [TestCase("daily-reward")]
        [TestCase("shop/offers/weekly_1")]
        public void IsValidRoute_LegitimateRoutes_Accepted(string route)
        {
            Assert.IsTrue(NotificationPayloadValidator.IsValidRoute(route));
        }

        [TestCase("evil://host")]
        [TestCase("shop?grant=1000")]
        [TestCase("shop#frag")]
        [TestCase("../../etc")]
        [TestCase("with space")]
        public void IsValidRoute_InjectionShapes_Rejected(string route)
        {
            Assert.IsFalse(NotificationPayloadValidator.IsValidRoute(route));
        }

        [Test]
        public void Validate_UnsupportedVersion_Rejected()
        {
            Assert.IsFalse(NotificationPayloadValidator.Validate(new NotificationPayload(route: "shop", version: 99), out _));
            Assert.IsFalse(NotificationPayloadValidator.Validate(new NotificationPayload(route: "shop", version: 0), out _));
        }

        [Test]
        public void Validate_TooManyParameters_Rejected()
        {
            var parameters = new Dictionary<string, string>();
            for (int i = 0; i <= NotificationPayloadValidator.MaxParameters; i++)
            {
                parameters["p" + i] = "v";
            }

            Assert.IsFalse(NotificationPayloadValidator.Validate(new NotificationPayload(route: "shop", parameters: parameters), out _));
        }

        [Test]
        public void Validate_OversizedParameterValue_Rejected()
        {
            var parameters = new Dictionary<string, string> { ["id"] = new string('x', NotificationPayloadValidator.MaxParameterValueLength + 1) };

            Assert.IsFalse(NotificationPayloadValidator.Validate(new NotificationPayload(route: "shop", parameters: parameters), out _));
        }

        [Test]
        public void Validate_WellFormedPayload_Accepted()
        {
            var payload = new NotificationPayload(type: "reminder", route: "daily-reward",
                parameters: new Dictionary<string, string> { ["source"] = "notification" });

            Assert.IsTrue(NotificationPayloadValidator.Validate(payload, out string reason), reason);
        }

        [Test]
        public void Service_MalformedInboundPayload_OpenStillReportedButPayloadDropped()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService events);
            var service = new NotificationService(new MockNotificationProvider(), new ManualNotificationClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            service.Initialize(registry);

            NotificationOpenedInfo received = default;
            bool raised = false;
            service.NotificationOpened += info =>
            {
                raised = true;
                received = info;
            };

            service.SimulateNotificationOpened(new NotificationId("n1"), new NotificationPayload(route: "evil://grant?coins=999999"));

            Assert.IsTrue(raised);
            Assert.AreEqual(string.Empty, received.Payload.Route);
            service.Shutdown();
        }

        [Test]
        public void Bridge_InvalidRoutePublishedDirectly_IsNotProcessed()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService events);
            var deepLinks = new DeepLinkService();
            deepLinks.Initialize(registry);
            deepLinks.SetReady(true);
            var handler = new FakeDeepLinkHandler();
            deepLinks.RegisterHandler(handler);
            var bridge = new NotificationDeepLinkBridge(events, deepLinks);

            // Bypasses NotificationService's own validation on purpose.
            events.Publish(new NotificationOpenedEvent(new NotificationOpenedInfo(
                new NotificationId("n1"), new NotificationPayload(route: "shop?grant=1"), wasSimulated: true)));

            Assert.AreEqual(0, handler.HandleCallCount);
            bridge.Dispose();
            deepLinks.Shutdown();
        }
    }
}
