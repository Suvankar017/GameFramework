using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 13's one
    /// application-level service (<see cref="IPlayerProfileService"/>) - the same registration-
    /// extension mechanism every other phase's bootstrapper subclass uses (compare
    /// <c>PerformanceBootstrapper</c>, <c>ProgressionBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>PlayerSystemsBootstrapper</c>/<c>PerformanceBootstrapper</c>/
    /// <c>ProgressionBootstrapper</c>: nothing in <see cref="GameFramework.PlayerData"/> references
    /// Input/UI/Audio/Feedback/Gameplay/GameFlow/Performance/Progression types - every dependency
    /// <see cref="PlayerProfileService"/> needs (<c>IPersistenceService</c>, <c>IEventService</c>,
    /// <c>ITimerService</c>, and optionally <c>ISceneService</c>/<c>ILoggingService</c>) is already
    /// part of the base eight services <see cref="GameBootstrapper"/> itself registers. A game
    /// wanting Player Data alongside any other phase combines them in its own small subclass, exactly
    /// as the framework already documents for combining any two of those:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry);
    ///         registry.Register&lt;IPlayerProfileService&gt;(new PlayerProfileService());
    ///     }
    /// }
    /// </code>
    ///
    /// Section registration is content, not composition - it happens after
    /// <see cref="GameBootstrapper.State"/> reaches <see cref="BootstrapState.Ready"/>, the same
    /// "wait for Ready, then wire content" pattern <c>ProgressionBootstrapper</c>'s own remarks
    /// document:
    /// <code>
    /// var playerData = GameBootstrapper.Instance.Services.Get&lt;IPlayerProfileService&gt;();
    /// playerData.RegisterSection(() =&gt; new ProgressionDataSection());
    /// playerData.LoadDefaultProfile();
    /// </code>
    /// </summary>
    public class PlayerDataBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IPlayerProfileService>(new PlayerProfileService());
        }
    }
}
