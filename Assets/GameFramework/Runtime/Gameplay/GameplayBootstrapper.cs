using GameFramework.Gameplay.Pooling;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 4's two
    /// application-level services (<see cref="IGameplayService"/>, <see cref="IPoolService"/>) —
    /// not another GameManager, just the same registration-extension mechanism every phase uses.
    ///
    /// Deliberately extends <see cref="GameBootstrapper"/> directly rather than
    /// <c>PlayerSystemsBootstrapper</c>: Gameplay Infrastructure and Player Experience (Input/UI/
    /// Audio/Feedback) are siblings that both sit on top of the Core Framework, neither depending
    /// on the other (see the framework's architecture diagram) — nothing in
    /// <see cref="GameFramework.Gameplay"/> ever references Input/UI/Audio/Feedback types
    /// directly. A game that wants both Player Systems and Gameplay Infrastructure combines them in
    /// its own small subclass instead:
    /// <code>
    /// public class MyGameBootstrapper : PlayerSystemsBootstrapper
    /// {
    ///     protected override void RegisterServices(IServiceRegistry registry)
    ///     {
    ///         base.RegisterServices(registry); // Phase 1/2/3
    ///         registry.Register&lt;IGameplayService&gt;(new GameplayService());
    ///         registry.Register&lt;IPoolService&gt;(new PoolService());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class GameplayBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IGameplayService>(new GameplayService());
            registry.Register<IPoolService>(new PoolService());
        }
    }
}
