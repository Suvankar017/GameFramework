using System.Collections.Generic;
using GameFramework.Performance.Mobile;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using NUnit.Framework;

namespace GameFramework.Analytics.Tests
{
    public class AnalyticsServiceTests
    {
        private EventService _events;
        private PersistenceService _persistence;
        private FakeAnalyticsProvider _provider;
        private AnalyticsService _analytics;

        private void SetUp(bool consentRequired = false, ConsentPolicy consentPolicy = ConsentPolicy.BufferUntilDecided,
            int maxParametersPerEvent = 25, int maxParameterValueLength = 100, float sessionTimeoutSeconds = 1800f)
        {
            var registry = TestRegistryFactory.Build(out _events, out _persistence);

            _provider = new FakeAnalyticsProvider();
            var config = TestDefinitions.AnalyticsConfig(
                consentRequired: consentRequired, consentPolicy: consentPolicy,
                maxParametersPerEvent: maxParametersPerEvent, maxParameterValueLength: maxParameterValueLength,
                sessionTimeoutSeconds: sessionTimeoutSeconds);

            _analytics = new AnalyticsService(config, _provider);
            _analytics.Initialize(registry);
        }

        [TearDown]
        public void TearDown() => _analytics?.Shutdown();

        [Test]
        public void Track_ValidEvent_ReachesProvider_WhenConsentGranted()
        {
            SetUp();
            _provider.TrackedEvents.Clear(); // discard the automatic session_start

            _analytics.Track("level_completed", new Dictionary<string, object> { ["level_id"] = "1-1" });

            Assert.AreEqual(1, _provider.TrackedEvents.Count);
            Assert.AreEqual("level_completed", _provider.TrackedEvents[0].Name);
        }

        [Test]
        public void Track_InvalidEventName_IsDropped()
        {
            SetUp();
            _provider.TrackedEvents.Clear();

            _analytics.Track("1_invalid_start");
            _analytics.Track("has space");
            _analytics.Track("");

            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Track_TooManyParameters_DropsExcessButKeepsEvent()
        {
            SetUp(maxParametersPerEvent: 2);
            _provider.TrackedEvents.Clear();

            _analytics.Track("custom_event", new Dictionary<string, object>
            {
                ["a"] = 1, ["b"] = 2, ["c"] = 3
            });

            Assert.AreEqual(1, _provider.TrackedEvents.Count);
            Assert.LessOrEqual(_provider.TrackedEvents[0].Parameters.Count, 2);
        }

        [Test]
        public void Track_UnsupportedParameterType_IsDropped()
        {
            SetUp();
            _provider.TrackedEvents.Clear();

            _analytics.Track("custom_event", new Dictionary<string, object>
            {
                ["valid"] = "ok",
                ["invalid"] = new object()
            });

            Assert.IsTrue(_provider.TrackedEvents[0].Parameters.ContainsKey("valid"));
            Assert.IsFalse(_provider.TrackedEvents[0].Parameters.ContainsKey("invalid"));
        }

        [Test]
        public void Track_LongStringParameter_IsTruncated()
        {
            SetUp(maxParameterValueLength: 5);
            _provider.TrackedEvents.Clear();

            _analytics.Track("custom_event", new Dictionary<string, object> { ["text"] = "abcdefghij" });

            Assert.AreEqual("abcde", _provider.TrackedEvents[0].Parameters["text"]);
        }

        [Test]
        public void Track_WhenDisabled_IsNoOp()
        {
            SetUp();
            _provider.TrackedEvents.Clear();

            _analytics.SetEnabled(false);
            _analytics.Track("custom_event");

            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Track_ConsentUnknown_BufferUntilDecided_QueuesThenFlushesOnGranted()
        {
            SetUp(consentRequired: true, consentPolicy: ConsentPolicy.BufferUntilDecided);
            // session_start itself was queued (consent is Unknown at Initialize).
            Assert.AreEqual(0, _provider.TrackedEvents.Count);
            Assert.AreEqual(1, _analytics.GetDiagnostics().QueuedEventCount);

            _analytics.Track("custom_event");
            Assert.AreEqual(2, _analytics.GetDiagnostics().QueuedEventCount);

            _analytics.SetConsent(ConsentState.Granted);

            Assert.AreEqual(2, _provider.TrackedEvents.Count);
            Assert.AreEqual(0, _analytics.GetDiagnostics().QueuedEventCount);
        }

        [Test]
        public void Track_ConsentUnknown_DropUntilGranted_DropsEvent()
        {
            SetUp(consentRequired: true, consentPolicy: ConsentPolicy.DropUntilGranted);

            _analytics.Track("custom_event");

            Assert.AreEqual(0, _analytics.GetDiagnostics().QueuedEventCount);

            _analytics.SetConsent(ConsentState.Granted);

            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void SetConsent_Denied_ClearsQueueWithoutSending()
        {
            SetUp(consentRequired: true, consentPolicy: ConsentPolicy.BufferUntilDecided);
            _analytics.Track("custom_event");
            Assert.Greater(_analytics.GetDiagnostics().QueuedEventCount, 0);

            _analytics.SetConsent(ConsentState.Denied);

            Assert.AreEqual(0, _analytics.GetDiagnostics().QueuedEventCount);

            _analytics.SetConsent(ConsentState.Granted);
            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Track_ProviderThrows_DoesNotThrow()
        {
            SetUp();
            _provider.ThrowOnTrackEvent = true;

            Assert.DoesNotThrow(() => _analytics.Track("custom_event"));
        }

        [Test]
        public void Identity_PersistsAcrossInstances_SharingStorage()
        {
            var storage = new InMemoryPersistenceStorage();
            var registry1 = TestRegistryFactory.Build(storage, out EventService events1, out PersistenceService persistence1);
            var config = TestDefinitions.AnalyticsConfig(consentRequired: false);
            var provider1 = new FakeAnalyticsProvider();
            var analytics1 = new AnalyticsService(config, provider1);
            analytics1.Initialize(registry1);
            string firstUserId = analytics1.UserId;
            analytics1.Shutdown();

            var registry2 = TestRegistryFactory.Build(storage, out EventService events2, out PersistenceService persistence2);
            var provider2 = new FakeAnalyticsProvider();
            var analytics2 = new AnalyticsService(config, provider2);
            analytics2.Initialize(registry2);

            Assert.AreEqual(firstUserId, analytics2.UserId);
            analytics2.Shutdown();
        }

        [Test]
        public void ResetIdentity_GeneratesNewId_AndInformsProvider()
        {
            SetUp();
            string originalId = _analytics.UserId;

            _analytics.ResetIdentity();

            Assert.AreNotEqual(originalId, _analytics.UserId);
            Assert.AreEqual(_analytics.UserId, _provider.UserId);
        }

        [Test]
        public void TrackScreenView_AddsScreenNameParameter()
        {
            SetUp();
            _provider.TrackedEvents.Clear();

            _analytics.TrackScreenView("MainMenu");

            Assert.AreEqual(EventNames.ScreenView, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("MainMenu", _provider.TrackedEvents[0].Parameters["screen_name"]);
        }

        [Test]
        public void SetUserProperty_ValidValue_ReachesProvider()
        {
            SetUp();

            _analytics.SetUserProperty("favorite_color", "blue");

            Assert.AreEqual("blue", _provider.UserProperties["favorite_color"]);
        }

        [Test]
        public void SetUserProperty_UnsupportedType_IsDropped()
        {
            SetUp();

            _analytics.SetUserProperty("bad", new object());

            Assert.IsFalse(_provider.UserProperties.ContainsKey("bad"));
        }

        [Test]
        public void Session_StartsOnInitialize()
        {
            SetUp();

            Assert.IsNotEmpty(_analytics.SessionId);
            Assert.AreEqual(EventNames.SessionStart, _provider.TrackedEvents[0].Name);
        }

        [Test]
        public void Session_PauseResume_WithinTimeout_KeepsSameSession()
        {
            SetUp(sessionTimeoutSeconds: 999999f);
            string originalSessionId = _analytics.SessionId;
            _provider.TrackedEvents.Clear();

            _events.Publish(new ApplicationPausedEvent());
            _events.Publish(new ApplicationResumedEvent());

            Assert.AreEqual(originalSessionId, _analytics.SessionId);
            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Session_PauseResume_BeyondTimeout_StartsNewSession()
        {
            SetUp(sessionTimeoutSeconds: 0f);
            string originalSessionId = _analytics.SessionId;
            _provider.TrackedEvents.Clear();

            _events.Publish(new ApplicationPausedEvent());
            _events.Publish(new ApplicationResumedEvent());

            Assert.AreNotEqual(originalSessionId, _analytics.SessionId);
            Assert.AreEqual(EventNames.SessionEnd, _provider.TrackedEvents[0].Name);
            Assert.AreEqual(EventNames.SessionStart, _provider.TrackedEvents[1].Name);
        }

        [Test]
        public void GetDiagnostics_ReflectsSentCount()
        {
            SetUp();
            int before = _analytics.GetDiagnostics().EventsSentCount;

            _analytics.Track("custom_event");

            Assert.AreEqual(before + 1, _analytics.GetDiagnostics().EventsSentCount);
        }
    }
}
