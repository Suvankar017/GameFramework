using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 8's one
    /// application-level service (<see cref="IGameFlowService"/>) - the same registration-extension
    /// mechanism every other phase's bootstrapper subclass uses (compare
    /// <c>GameplayBootstrapper</c>, <c>PerformanceBootstrapper</c>).
    ///
    /// <see cref="GameFlowService"/>'s use of <see cref="Gameplay.IGameplayService"/> (to start/stop
    /// ticking gameplay participants alongside a session) is a soft dependency resolved via
    /// <c>registry.TryGet</c> - <see cref="IGameFlowService"/> works fully without Gameplay
    /// Infrastructure registered at all, it just then skips those calls. Sibling, not a base or
    /// subclass, of <c>GameplayBootstrapper</c>/<c>PerformanceBootstrapper</c>/
    /// <c>PlayerSystemsBootstrapper</c>: nothing in <see cref="GameFramework.GameFlow"/> references
    /// Input/UI/Audio/Feedback/Performance/Progression/Unlocks/Rewards/Quests types. A game wanting
    /// Game Flow alongside any of those combines them in its own small subclass, exactly as the
    /// framework already documents for combining any two of those:
    /// <code>
    /// public class MyGameBootstrapper : GameplayBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1-4 (includes IGameplayService)
    ///         registry.Register&lt;IGameFlowService&gt;(new GameFlowService());
    ///     }
    /// }
    /// </code>
    /// Register <see cref="IGameFlowService"/> *after* <see cref="Gameplay.IGameplayService"/> if
    /// both are wanted - registration order determines what a soft <c>TryGet</c> can actually find,
    /// exactly as documented throughout this framework (see <c>GameBootstrapper.RegisterServices</c>'s
    /// remarks).
    /// </summary>
    public class GameFlowBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IGameFlowService>(new GameFlowService());
        }
    }
}
