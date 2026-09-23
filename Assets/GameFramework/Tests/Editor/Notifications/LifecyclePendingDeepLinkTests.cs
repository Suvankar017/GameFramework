using GameFramework.DeepLinks;
using GameFramework.Notifications.Integration;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.UI.Navigation;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Covers CLAUDE.md's Phase 18 brief, section 46's second required "Integration" test:
    /// <c>Lifecycle -&gt; Pending DeepLink -&gt; Navigation</c>. A cold-start-style link arrives before
    /// <see cref="IDeepLinkService.SetReady"/> is called (before UI/GameFlow readiness); it must be
    /// held, not dropped or processed early, and dispatched exactly once the game signals readiness.</summary>
    public class LifecyclePendingDeepLinkTests
    {
        private static readonly UIScreenId DailyRewardScreen = new UIScreenId("DailyReward");

        private DeepLinkService _deepLinks;
        private FakeNavigationService _navigation;
        private NavigationDeepLinkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService _);
            _deepLinks = new DeepLinkService();
            _deepLinks.Initialize(registry);
            // Deliberately NOT calling SetReady(true) here - simulates cold start, before the game's
            // own UI/GameFlow readiness signal arrives.

            _navigation = new FakeNavigationService();
            _handler = new NavigationDeepLinkHandler(_navigation);
            _handler.MapRoute("daily-reward", DailyRewardScreen);
            _deepLinks.RegisterHandler(_handler);
        }

        [TearDown]
        public void TearDown() => _deepLinks.Shutdown();

        [Test]
        public void LinkArrivingBeforeReady_IsHeldAndNeverNavigatesEarly()
        {
            DeepLinkResult result = _deepLinks.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Deferred, result.Kind);
            Assert.AreEqual(0, _navigation.NavigateCalls.Count);
            Assert.AreEqual("mygame://daily-reward", _deepLinks.PendingRawUri);
        }

        [Test]
        public void GameFlowReadySignal_DispatchesPendingLinkToNavigation()
        {
            _deepLinks.Process("mygame://daily-reward");

            // The game's own startup flow (Bootstrap -> Main Menu ready, or GameFlow reaching its
            // ready state) signals readiness - see CLAUDE.md's Phase 18 brief, section 40.
            _deepLinks.SetReady(true);

            Assert.AreEqual(1, _navigation.NavigateCalls.Count);
            Assert.AreEqual(DailyRewardScreen, _navigation.NavigateCalls[0].Destination);
            Assert.IsNull(_deepLinks.PendingRawUri);
        }

        [Test]
        public void AlreadyReady_ProcessesImmediatelyWithoutDeferring()
        {
            _deepLinks.SetReady(true);

            DeepLinkResult result = _deepLinks.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Handled, result.Kind);
            Assert.AreEqual(1, _navigation.NavigateCalls.Count);
        }
    }
}
