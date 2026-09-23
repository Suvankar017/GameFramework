using System;
using GameFramework.Notifications.Providers.Mock;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    public class NotificationServiceTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private MockNotificationProvider _provider;
        private ManualNotificationClock _clock;
        private NotificationService _service;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _events);
            _provider = new MockNotificationProvider();
            _clock = new ManualNotificationClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _service = new NotificationService(_provider, _clock);
            _service.Initialize(_registry);
            _provider.RequestPermission(_ => { }); // Authorize by default for scheduling tests.
        }

        [TearDown]
        public void TearDown() => _service.Shutdown();

        private static NotificationRequest MakeRequest(string id, DateTime scheduledUtc, NotificationPayload payload = null) =>
            new NotificationRequest(new NotificationId(id), NotificationContent.FromText("Title", "Body"), scheduledUtc, payload: payload);

        [Test]
        public void Schedule_Valid_Succeeds()
        {
            NotificationResult result = _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(_service.IsScheduled(new NotificationId("n1")));
        }

        [Test]
        public void Schedule_PastTime_ReturnsInvalidRequest()
        {
            NotificationResult result = _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(-5)));

            Assert.AreEqual(NotificationResultKind.InvalidRequest, result.Kind);
        }

        [Test]
        public void Schedule_EmptyId_ReturnsInvalidRequest()
        {
            NotificationResult result = _service.Schedule(MakeRequest(string.Empty, _clock.UtcNow.AddMinutes(5)));

            Assert.AreEqual(NotificationResultKind.InvalidRequest, result.Kind);
        }

        [Test]
        public void Schedule_NullRequest_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Schedule(null));
        }

        [Test]
        public void Schedule_PermissionNotGranted_ReturnsPermissionDenied()
        {
            var freshProvider = new MockNotificationProvider(); // NotDetermined by default, never requested
            var freshService = new NotificationService(freshProvider, _clock);
            freshService.Initialize(TestRegistryFactory.Build(out EventService _));

            NotificationResult result = freshService.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));

            Assert.AreEqual(NotificationResultKind.PermissionDenied, result.Kind);
            freshService.Shutdown();
        }

        [Test]
        public void Schedule_Unsupported_ReturnsUnsupported()
        {
            var unsupportedProvider = new MockNotificationProvider(MockNotificationSimulationMode.AlwaysUnsupported);
            var service = new NotificationService(unsupportedProvider, _clock);
            service.Initialize(TestRegistryFactory.Build(out EventService _));

            NotificationResult result = service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));

            Assert.AreEqual(NotificationResultKind.Unsupported, result.Kind);
            service.Shutdown();
        }

        [Test]
        public void Schedule_DuplicateId_ReplacesExisting()
        {
            _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));
            NotificationResult result = _service.Schedule(new NotificationRequest(
                new NotificationId("n1"), NotificationContent.FromText("New Title", "New Body"), _clock.UtcNow.AddMinutes(10)));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, _service.GetScheduled().Count);
            Assert.AreEqual("New Title", _service.GetScheduled()[0].Content.Title.RawText);
        }

        [Test]
        public void Schedule_PublishesNotificationScheduledEvent()
        {
            bool published = false;
            _events.Subscribe<NotificationScheduledEvent>(_ => published = true);

            _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));

            Assert.IsTrue(published);
        }

        [Test]
        public void Cancel_Scheduled_Succeeds_AndPublishesEvent()
        {
            _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));

            bool published = false;
            _events.Subscribe<NotificationCancelledEvent>(_ => published = true);

            NotificationResult result = _service.Cancel(new NotificationId("n1"));

            Assert.AreEqual(NotificationResultKind.Cancelled, result.Kind);
            Assert.IsFalse(_service.IsScheduled(new NotificationId("n1")));
            Assert.IsTrue(published);
        }

        [Test]
        public void Cancel_NotScheduled_ReturnsNotFound()
        {
            NotificationResult result = _service.Cancel(new NotificationId("does-not-exist"));

            Assert.AreEqual(NotificationResultKind.NotFound, result.Kind);
        }

        [Test]
        public void CancelAll_ClearsEverything()
        {
            _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5)));
            _service.Schedule(MakeRequest("n2", _clock.UtcNow.AddMinutes(10)));

            _service.CancelAll();

            Assert.AreEqual(0, _service.GetScheduled().Count);
        }

        [Test]
        public void Localization_ResolvesKeyAtScheduleTime()
        {
            var localization = new FakeLocalizationService();
            localization.Set("notif.title", "Localized Title");
            localization.Set("notif.body", "Localized Body");

            ServiceRegistry registry = TestRegistryFactory.Build(out EventService _);
            registry.Register<Localization.ILocalizationService>(localization);
            registry.MarkInitialized(typeof(Localization.ILocalizationService));

            var provider = new MockNotificationProvider();
            var service = new NotificationService(provider, _clock);
            service.Initialize(registry);
            provider.RequestPermission(_ => { });

            var request = new NotificationRequest(
                new NotificationId("n1"),
                new NotificationContent(NotificationText.FromLocalizationKey("notif.title"), NotificationText.FromLocalizationKey("notif.body")),
                _clock.UtcNow.AddMinutes(5));

            service.Schedule(request);

            NotificationRequest scheduled = service.GetScheduled()[0];
            Assert.AreEqual("Localized Title", scheduled.Content.Title.RawText);
            Assert.AreEqual("Localized Body", scheduled.Content.Body.RawText);

            service.Shutdown();
        }

        [Test]
        public void Localization_NoServiceRegistered_FallsBackToKeyText()
        {
            var request = new NotificationRequest(
                new NotificationId("n1"),
                new NotificationContent(NotificationText.FromLocalizationKey("notif.title"), NotificationText.FromText("Body")),
                _clock.UtcNow.AddMinutes(5));

            _service.Schedule(request);

            Assert.AreEqual("notif.title", _service.GetScheduled()[0].Content.Title.RawText);
        }

        [Test]
        public void ColdStartLaunchNotification_RaisesNotificationOpenedDuringInitialize()
        {
            var provider = new MockNotificationProvider();
            var payload = new NotificationPayload(route: "daily-reward");
            provider.SimulateColdStartLaunch(new NotificationId("n1"), payload);

            NotificationOpenedInfo? openedInfo = null;
            var service = new NotificationService(provider, _clock);
            service.NotificationOpened += info => openedInfo = info;
            service.Initialize(TestRegistryFactory.Build(out EventService _));

            Assert.IsTrue(openedInfo.HasValue);
            Assert.AreEqual("n1", openedInfo.Value.Id.Value);
            Assert.AreEqual("daily-reward", openedInfo.Value.Payload.Route);
            Assert.IsTrue(openedInfo.Value.WasSimulated);

            service.Shutdown();
        }

        [Test]
        public void SimulateNotificationOpened_RaisesEvent_MarkedAsSimulated()
        {
            _service.Schedule(MakeRequest("n1", _clock.UtcNow.AddMinutes(5), new NotificationPayload(route: "shop")));

            NotificationOpenedInfo? info = null;
            _service.NotificationOpened += i => info = i;

            _service.SimulateNotificationOpened(new NotificationId("n1"));

            Assert.IsTrue(info.HasValue);
            Assert.AreEqual("shop", info.Value.Payload.Route);
            Assert.IsTrue(info.Value.WasSimulated);
        }

        [Test]
        public void SimulateNotificationReceived_PublishesReceivedEvent()
        {
            bool published = false;
            _events.Subscribe<NotificationReceivedEvent>(_ => published = true);

            _service.SimulateNotificationReceived(new NotificationId("n1"));

            Assert.IsTrue(published);
        }

        [Test]
        public void RequestPermission_Authorized_UpdatesStatusAndRaisesChangedEvent()
        {
            var freshProvider = new MockNotificationProvider();
            var freshService = new NotificationService(freshProvider, _clock);
            freshService.Initialize(TestRegistryFactory.Build(out EventService events));

            bool changedPublished = false;
            events.Subscribe<NotificationPermissionChangedEvent>(_ => changedPublished = true);

            NotificationPermissionStatus? result = null;
            freshService.RequestPermission(status => result = status);

            Assert.AreEqual(NotificationPermissionStatus.Authorized, result);
            Assert.AreEqual(NotificationPermissionStatus.Authorized, freshService.PermissionStatus);
            Assert.IsTrue(changedPublished);

            freshService.Shutdown();
        }

        [Test]
        public void RequestPermission_Denied_ReportsDenied()
        {
            var provider = new MockNotificationProvider(MockNotificationSimulationMode.AlwaysPermissionDenied);
            var service = new NotificationService(provider, _clock);
            service.Initialize(TestRegistryFactory.Build(out EventService _));

            NotificationPermissionStatus? result = null;
            service.RequestPermission(status => result = status);

            Assert.AreEqual(NotificationPermissionStatus.Denied, result);
            service.Shutdown();
        }
    }
}
