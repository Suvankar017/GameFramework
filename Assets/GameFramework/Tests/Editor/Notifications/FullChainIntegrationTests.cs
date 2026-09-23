using System;
using System.Collections.Generic;
using GameFramework.DeepLinks;
using GameFramework.Notifications.Integration;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.UI.Navigation;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Covers CLAUDE.md's Phase 18 brief, section 46's "Integration" test requirement: the
    /// full <c>Notification -&gt; Payload -&gt; DeepLink -&gt; Navigation</c> chain, wired exactly the
    /// way a game would (no shortcuts into internals).</summary>
    public class FullChainIntegrationTests
    {
        private static readonly UIScreenId DailyRewardScreen = new UIScreenId("DailyReward");

        private EventService _events;
        private NotificationService _notifications;
        private DeepLinkService _deepLinks;
        private FakeNavigationService _navigation;
        private NotificationDeepLinkBridge _bridge;
        private NavigationDeepLinkHandler _navigationHandler;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out _events);

            var provider = new Providers.Mock.MockNotificationProvider();
            _notifications = new NotificationService(provider);
            _notifications.Initialize(registry);
            provider.RequestPermission(_ => { });

            _deepLinks = new DeepLinkService();
            _deepLinks.Initialize(registry);
            _deepLinks.SetReady(true);

            _navigation = new FakeNavigationService();
            _navigationHandler = new NavigationDeepLinkHandler(_navigation);
            _navigationHandler.MapRoute("daily-reward", DailyRewardScreen);
            _deepLinks.RegisterHandler(_navigationHandler);

            _bridge = new NotificationDeepLinkBridge(_events, _deepLinks);
        }

        [TearDown]
        public void TearDown()
        {
            _bridge.Dispose();
            _deepLinks.Shutdown();
            _notifications.Shutdown();
        }

        [Test]
        public void TappingScheduledNotification_NavigatesToMappedScreen()
        {
            var payload = new NotificationPayload(type: "daily_reward", route: "daily-reward",
                parameters: new Dictionary<string, string> { ["source"] = "notification" });

            _notifications.Schedule(new NotificationRequest(
                new NotificationId("daily_reward_reminder"), NotificationContent.FromText("Come back!", "Your reward is waiting."),
                DateTime.UtcNow.AddMinutes(5), payload: payload));

            // The user taps the notification - the shipped provider reports it as opened.
            _notifications.SimulateNotificationOpened(new NotificationId("daily_reward_reminder"));

            Assert.AreEqual(1, _navigation.NavigateCalls.Count);
            Assert.AreEqual(DailyRewardScreen, _navigation.NavigateCalls[0].Destination);

            var parameters = (DeepLinkNavigationParameters)_navigation.NavigateCalls[0].Options.Parameters;
            Assert.AreEqual("notification", parameters.Link.QueryParameters["source"]);
        }

        [Test]
        public void TappingNotificationWithUnroutedPayload_DoesNotNavigate()
        {
            _notifications.Schedule(new NotificationRequest(
                new NotificationId("n1"), NotificationContent.FromText("Hi", "There"), DateTime.UtcNow.AddMinutes(5)));

            _notifications.SimulateNotificationOpened(new NotificationId("n1"));

            Assert.AreEqual(0, _navigation.NavigateCalls.Count);
        }
    }
}
