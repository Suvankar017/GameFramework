using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 9's one
    /// application-level service (<see cref="ITutorialService"/>) - the same registration-extension
    /// mechanism every other phase's bootstrapper subclass uses (compare
    /// <c>GameFlowBootstrapper</c>, <c>PerformanceBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>GameFlowBootstrapper</c>/<c>GameplayBootstrapper</c>/
    /// <c>PerformanceBootstrapper</c>/<c>PlayerSystemsBootstrapper</c>: nothing in
    /// <see cref="GameFramework.Tutorials"/> references GameFlow/Gameplay/Performance/Progression/
    /// Unlocks/Rewards/Quests/UI/Localization/Audio/Feedback types - only Core/Runtime/Input. A
    /// game wanting tutorials alongside any of those combines them in its own small subclass,
    /// exactly as the framework already documents for combining any two of those:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1-3, includes IInputService
    ///         registry.Register&lt;ITutorialService&gt;(new TutorialService());
    ///     }
    /// }
    /// </code>
    /// Register <see cref="ITutorialService"/> *after* <c>IInputService</c> if input-gated/
    /// input-step tutorials are wanted - registration order determines what a soft
    /// <c>registry.TryGet</c> can actually find, exactly as documented throughout this framework.
    /// <see cref="ITutorialService"/> works fully without <c>IInputService</c> registered at all
    /// (it just then never gates input and any <see cref="Steps.InputStep"/> a game registers stays
    /// permanently un-completable, which is a content-authoring mistake, not a framework failure).
    /// </summary>
    public class TutorialBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ITutorialService>(new TutorialService());
        }
    }
}
