using GameFramework.DeepLinks;
using GameFramework.Notifications.Integration;
using GameFramework.UI.Navigation;
using NUnit.Framework;

namespace GameFramework.Notifications.Tests
{
    public class NavigationDeepLinkHandlerTests
    {
        private static readonly UIScreenId DailyRewardScreen = new UIScreenId("DailyReward");
        private static readonly UIScreenId ShopScreen = new UIScreenId("Shop");

        private FakeNavigationService _navigation;
        private NavigationDeepLinkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _navigation = new FakeNavigationService();
            _handler = new NavigationDeepLinkHandler(_navigation);
            _handler.MapRoute("daily-reward", DailyRewardScreen);
            _handler.MapRoute("shop/{itemId}", ShopScreen);
        }

        [Test]
        public void CanHandle_MatchingRoute_ReturnsTrue()
        {
            DeepLinkParser.TryParse("mygame://daily-reward", out DeepLink link);
            Assert.IsTrue(_handler.CanHandle(link));
        }

        [Test]
        public void CanHandle_UnmatchedRoute_ReturnsFalse()
        {
            DeepLinkParser.TryParse("mygame://unknown", out DeepLink link);
            Assert.IsFalse(_handler.CanHandle(link));
        }

        [Test]
        public void Handle_MatchingRoute_CallsNavigateWithCorrectScreen()
        {
            DeepLinkParser.TryParse("mygame://daily-reward", out DeepLink link);

            DeepLinkHandlerResult result = _handler.Handle(link);

            Assert.AreEqual(DeepLinkHandlerResult.Handled, result);
            Assert.AreEqual(1, _navigation.NavigateCalls.Count);
            Assert.AreEqual(DailyRewardScreen, _navigation.NavigateCalls[0].Destination);
        }

        [Test]
        public void Handle_RouteWithPathParameter_PassesParametersThrough()
        {
            DeepLinkParser.TryParse("mygame://shop/sword-of-truth", out DeepLink link);

            _handler.Handle(link);

            var parameters = (DeepLinkNavigationParameters)_navigation.NavigateCalls[0].Options.Parameters;
            Assert.AreEqual("sword-of-truth", parameters.PathParameters["itemId"]);
        }

        [Test]
        public void Handle_NavigationFails_ReturnsFailed()
        {
            _navigation.NextResult = NavigationResult.Block("guard rejected");
            DeepLinkParser.TryParse("mygame://daily-reward", out DeepLink link);

            DeepLinkHandlerResult result = _handler.Handle(link);

            Assert.AreEqual(DeepLinkHandlerResult.Failed, result);
        }

        [Test]
        public void Handle_NoMatchingRoute_ReturnsNotApplicable()
        {
            DeepLinkParser.TryParse("mygame://unknown", out DeepLink link);

            DeepLinkHandlerResult result = _handler.Handle(link);

            Assert.AreEqual(DeepLinkHandlerResult.NotApplicable, result);
            Assert.AreEqual(0, _navigation.NavigateCalls.Count);
        }
    }
}
