using GameFramework.PlayerSystems;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Drop-in <see cref="PlayerSystemsBootstrapper"/> that additionally registers Phase 12's one
    /// application-level service (<see cref="INavigationService"/>) - the same registration-
    /// extension mechanism every other phase's bootstrapper subclass uses, but subclassing
    /// <see cref="PlayerSystemsBootstrapper"/> directly (not <see cref="GameBootstrapper"/>) because
    /// <see cref="NavigationService"/> has a genuine hard dependency on <see cref="IUIService"/> (it
    /// orchestrates Phase 3's screen/popup stack directly, it doesn't merely soft-look-up an
    /// optional channel the way <c>Presentation.PresentationService</c> does) - the same reasoning
    /// <c>Quests.QuestsBootstrapper</c> subclasses <c>Rewards.ProgressionBootstrapper</c> directly
    /// for its own hard dependency on Rewards.
    ///
    /// <c>GameFlow.IGameFlowService</c> (pause-token integration on popups) and
    /// <c>Input.IInputService</c> (Android/back-button routing, transition input blocking) are
    /// resolved softly via <c>registry.TryGet</c> - register them *before*
    /// <see cref="INavigationService"/> if wanted, in a game's own combined subclass:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1-3, includes IUIService/IInputService
    ///         registry.Register&lt;IGameFlowService&gt;(new GameFlowService());
    ///         registry.Register&lt;INavigationService&gt;(new NavigationService());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class NavigationBootstrapper : PlayerSystemsBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<INavigationService>(new NavigationService());
        }
    }
}
