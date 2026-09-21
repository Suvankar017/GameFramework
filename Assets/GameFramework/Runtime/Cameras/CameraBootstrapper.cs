using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 11's
    /// <see cref="ICameraService"/> - the same registration-extension mechanism every other phase's
    /// bootstrapper subclass uses (compare <c>Performance.PerformanceBootstrapper</c>,
    /// <c>GameFlow.GameFlowBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>PlayerSystemsBootstrapper</c>/<c>Gameplay.GameplayBootstrapper</c>/
    /// <c>Performance.PerformanceBootstrapper</c>: nothing in <see cref="GameFramework.Cameras"/>
    /// references Input/UI/Audio/Feedback/Gameplay/GameFlow types, so it stays usable by any game
    /// regardless of which of those it also uses. A game wanting Cameras alongside other systems
    /// combines them in its own small subclass, exactly as the framework already documents for every
    /// other pair:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1-3
    ///         registry.Register&lt;ICameraService&gt;(new CameraService());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class CameraBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ICameraService>(new CameraService());
        }
    }
}
