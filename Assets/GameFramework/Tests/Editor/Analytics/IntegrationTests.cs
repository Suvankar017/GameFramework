using GameFramework.Analytics.Integration;
using GameFramework.GameFlow;
using GameFramework.GameFlow.Session;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Purchases;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials;
using GameFramework.UI.Navigation;
using NUnit.Framework;

namespace GameFramework.Analytics.Tests
{
    /// <summary>Covers <c>Analytics.Integration</c>'s opt-in bridges (CLAUDE.md's Phase 16 brief,
    /// section 73's "Integration" checklist). Each test publishes the exact struct another phase's
    /// real service would publish - no real GameFlow/Navigation/Tutorial/Monetization service needs
    /// to be constructed, since a bridge only ever depends on the event type, never the publisher
    /// (see <see cref="GameFlowAnalyticsIntegration"/>'s remarks).</summary>
    public class IntegrationTests
    {
        private EventService _events;
        private AnalyticsService _analytics;
        private FakeAnalyticsProvider _provider;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out _events, out PersistenceService _);
            _provider = new FakeAnalyticsProvider();
            _analytics = new AnalyticsService(TestDefinitions.AnalyticsConfig(consentRequired: false), _provider);
            _analytics.Initialize(registry);
            _provider.TrackedEvents.Clear(); // discard the automatic session_start
        }

        [TearDown]
        public void TearDown() => _analytics.Shutdown();

        [Test]
        public void GameFlow_LevelStarted_TracksEvent()
        {
            using var bridge = new GameFlowAnalyticsIntegration(_events, _analytics);

            _events.Publish(new LevelStartedEvent(new LevelId("1-1"), SessionId.None, 1));

            Assert.AreEqual(1, _provider.TrackedEvents.Count);
            Assert.AreEqual(EventNames.LevelStarted, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("1-1", _provider.TrackedEvents[0].Parameters["level_id"]);
        }

        [Test]
        public void GameFlow_Dispose_StopsForwardingEvents()
        {
            var bridge = new GameFlowAnalyticsIntegration(_events, _analytics);
            bridge.Dispose();

            _events.Publish(new LevelStartedEvent(new LevelId("1-1"), SessionId.None, 1));

            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Navigation_ScreenNavigated_TracksScreenView()
        {
            using var bridge = new NavigationAnalyticsIntegration(_events, _analytics);

            _events.Publish(new ScreenNavigatedEvent(UIScreenId.None, new UIScreenId("MainMenu"), NavigationMode.Push));

            Assert.AreEqual(EventNames.ScreenView, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("MainMenu", _provider.TrackedEvents[0].Parameters["screen_name"]);
        }

        [Test]
        public void Navigation_PopupsNotTrackedByDefault()
        {
            using var bridge = new NavigationAnalyticsIntegration(_events, _analytics);

            _events.Publish(new PopupOpenedEvent(new UIPopupId("Confirm")));

            Assert.AreEqual(0, _provider.TrackedEvents.Count);
        }

        [Test]
        public void Tutorial_Completed_TracksEvent()
        {
            using var bridge = new TutorialAnalyticsIntegration(_events, _analytics);

            _events.Publish(new TutorialCompletedEvent(new TutorialId("Intro")));

            Assert.AreEqual(EventNames.TutorialCompleted, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("Intro", _provider.TrackedEvents[0].Parameters["tutorial_id"]);
        }

        [Test]
        public void Monetization_PurchaseCompleted_TracksNormalizedEvent()
        {
            using var bridge = new MonetizationAnalyticsIntegration(_events, _analytics);

            var result = PurchaseResult.Immediate(PurchaseResultKind.Success, new ProductId("remove_ads"));
            _events.Publish(new PurchaseCompletedEvent(result));

            Assert.AreEqual(EventNames.PurchaseCompleted, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("remove_ads", _provider.TrackedEvents[0].Parameters["product_id"]);
            Assert.AreEqual("Success", _provider.TrackedEvents[0].Parameters["result"]);
        }

        [Test]
        public void Monetization_AdRewardEarned_TracksEvent()
        {
            using var bridge = new MonetizationAnalyticsIntegration(_events, _analytics);

            _events.Publish(new AdRewardEarnedEvent(new AdPlacementId("RewardedRevive")));

            Assert.AreEqual(EventNames.AdRewardEarned, _provider.TrackedEvents[0].Name);
            Assert.AreEqual("RewardedRevive", _provider.TrackedEvents[0].Parameters["placement_id"]);
        }
    }
}
