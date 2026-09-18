using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 10's one
    /// application-level service (<see cref="IPresentationService"/>) - the same registration-
    /// extension mechanism every other phase's bootstrapper subclass uses (compare
    /// <c>TutorialBootstrapper</c>, <c>GameFlowBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>PlayerSystemsBootstrapper</c>/<c>GameplayBootstrapper</c>/
    /// <c>PerformanceBootstrapper</c>: every one of <see cref="PresentationService"/>'s Phase 3/4
    /// dependencies (<c>IAudioService</c>, <c>Feedback.IFeedbackService</c>, <c>UI.IUIService</c>,
    /// <c>Gameplay.Pooling.IPoolService</c>) is resolved softly via <c>registry.TryGet</c> - the
    /// service degrades channel by channel (logged once per missing channel) rather than requiring
    /// every one of those systems to be registered. A game combines Presentation with whichever of
    /// those it actually uses (recommended - most of the seven channels need at least one) in its
    /// own small subclass:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1-3, includes Audio/Feedback/UI
    ///         registry.Register&lt;IPresentationService&gt;(new PresentationService());
    ///     }
    /// }
    /// </code>
    /// Register <see cref="IPresentationService"/> *after* whichever of Audio/Feedback/UI/Pooling
    /// are wanted, so its soft lookups succeed - registration order determines what
    /// <c>registry.TryGet</c> can actually find, exactly as documented throughout this framework.
    /// </summary>
    public class PresentationBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IPresentationService>(new PresentationService());
        }
    }
}
