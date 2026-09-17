using GameFramework.Performance.Mobile;
using GameFramework.Performance.Profiling;
using GameFramework.Performance.Ticking;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Performance
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 5's four
    /// application-level services (<see cref="ITickService"/>, <see cref="IPerformanceMonitorService"/>,
    /// <see cref="IApplicationLifecycleService"/>, <see cref="IMobilePerformanceService"/>) - the
    /// same registration-extension mechanism every other phase's bootstrapper subclass uses
    /// (compare <c>PlayerSystemsBootstrapper</c>, <c>GameplayBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>PlayerSystemsBootstrapper</c>/<c>GameplayBootstrapper</c>:
    /// nothing in <see cref="GameFramework.Performance"/> references Input/UI/Audio/Feedback/Gameplay
    /// types, so it stays usable by any game regardless of which of those it also uses. A game
    /// wanting Performance alongside Player Systems and/or Gameplay Infrastructure combines them in
    /// its own small subclass, exactly as the framework already documents for combining those two:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1/2/3
    ///         registry.Register&lt;IGameplayService&gt;(new GameplayService());
    ///         registry.Register&lt;IPoolService&gt;(new PoolService());
    ///         registry.Register&lt;ITickService&gt;(new TickService());
    ///         registry.Register&lt;IPerformanceMonitorService&gt;(new PerformanceMonitorService());
    ///         registry.Register&lt;IApplicationLifecycleService&gt;(new ApplicationLifecycleService());
    ///         registry.Register&lt;IMobilePerformanceService&gt;(new MobilePerformanceService());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class PerformanceBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ITickService>(new TickService());
            registry.Register<IPerformanceMonitorService>(new PerformanceMonitorService());
            registry.Register<IApplicationLifecycleService>(new ApplicationLifecycleService());
            registry.Register<IMobilePerformanceService>(new MobilePerformanceService());
        }
    }
}
