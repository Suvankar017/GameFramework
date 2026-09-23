using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 18's
    /// <see cref="IDeepLinkService"/> - the same registration-extension mechanism every other phase's
    /// bootstrapper subclass uses.
    ///
    /// A bare <see cref="GameBootstrapper"/> subclass, not a <see cref="GameFramework.PlayerSystems.PlayerSystemsBootstrapper"/>
    /// subclass: <see cref="DeepLinkService"/> has no hard dependency on Input/UI - see
    /// <see cref="IDeepLinkHandler"/>'s remarks on why navigation wiring is an opt-in, separate
    /// concern (<c>Notifications.Integration.NavigationDeepLinkHandler</c>), not something this
    /// assembly references directly. Register <see cref="IDeepLinkService"/> in a game's own combined
    /// subclass alongside <c>UI.Navigation.NavigationBootstrapper</c> if navigation-driven routing is
    /// wanted.
    /// </summary>
    public class DeepLinksBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IDeepLinkService>(new DeepLinkService());
        }
    }
}
