using System.Collections.Generic;
using GameFramework.DeepLinks;

namespace GameFramework.Notifications.Integration
{
    /// <summary>Delivered as <see cref="UI.Navigation.NavigationRequestOptions.Parameters"/> by
    /// <see cref="NavigationDeepLinkHandler"/> - the destination screen casts to this type the same
    /// explicit way every other <c>INavigationService</c> parameter object is consumed (see
    /// CLAUDE.md's Phase 12 brief, section 19).</summary>
    public readonly struct DeepLinkNavigationParameters
    {
        public readonly DeepLink Link;
        public readonly IReadOnlyDictionary<string, string> PathParameters;

        public DeepLinkNavigationParameters(DeepLink link, IReadOnlyDictionary<string, string> pathParameters)
        {
            Link = link;
            PathParameters = pathParameters;
        }
    }
}
