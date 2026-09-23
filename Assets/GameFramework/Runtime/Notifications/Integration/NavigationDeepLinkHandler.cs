using System;
using System.Collections.Generic;
using GameFramework.DeepLinks;
using GameFramework.UI.Navigation;

namespace GameFramework.Notifications.Integration
{
    /// <summary>
    /// A ready-made <see cref="IDeepLinkHandler"/> that maps a route path pattern to a registered
    /// <see cref="UIScreenId"/> and calls <see cref="INavigationService.Navigate"/> - see CLAUDE.md's
    /// Phase 18 brief, section 24. Optional, game-constructed, never registered automatically -
    /// <see cref="GameFramework.DeepLinks"/> stays Navigation-agnostic (see
    /// <see cref="IDeepLinkHandler"/>'s own remarks); a game that wants this convenience registers one
    /// instance with <see cref="IDeepLinkService.RegisterHandler"/> and maps its routes:
    /// <code>
    /// var handler = new NavigationDeepLinkHandler(navigationService);
    /// handler.MapRoute("daily-reward", DailyRewardScreenId);
    /// handler.MapRoute("shop/{itemId}", ShopScreenId);
    /// deepLinkService.RegisterHandler(handler);
    /// </code>
    /// The destination screen reads <see cref="DeepLinkNavigationParameters"/> from
    /// <see cref="NavigationRequestOptions.Parameters"/> if it needs the link's path/query parameters.
    /// </summary>
    public sealed class NavigationDeepLinkHandler : IDeepLinkHandler
    {
        private readonly INavigationService _navigation;
        private readonly List<(DeepLinkRoutePattern Pattern, UIScreenId Screen)> _routes = new List<(DeepLinkRoutePattern, UIScreenId)>();

        public NavigationDeepLinkHandler(INavigationService navigation)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        public void MapRoute(string pattern, UIScreenId screen) => _routes.Add((new DeepLinkRoutePattern(pattern), screen));

        public bool CanHandle(DeepLink link)
        {
            for (int i = 0; i < _routes.Count; i++)
            {
                if (_routes[i].Pattern.TryMatch(link, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public DeepLinkHandlerResult Handle(DeepLink link)
        {
            for (int i = 0; i < _routes.Count; i++)
            {
                if (!_routes[i].Pattern.TryMatch(link, out IReadOnlyDictionary<string, string> pathParameters))
                {
                    continue;
                }

                var parameters = new DeepLinkNavigationParameters(link, pathParameters);
                NavigationResult result = _navigation.Navigate(_routes[i].Screen, new NavigationRequestOptions(parameters: parameters));
                return result.Success ? DeepLinkHandlerResult.Handled : DeepLinkHandlerResult.Failed;
            }

            return DeepLinkHandlerResult.NotApplicable;
        }
    }
}
