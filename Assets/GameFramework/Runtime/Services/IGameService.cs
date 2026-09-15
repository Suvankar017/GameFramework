namespace GameFramework.Runtime.Services
{
    /// <summary>
    /// Lifecycle contract for an application-level service owned by an <see cref="IServiceRegistry"/>.
    /// The bootstrap that registers a service calls Initialize exactly once (in registration
    /// order) and Shutdown exactly once (in reverse registration order); a service must not call
    /// either method on itself.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Called once by the bootstrap after registration. Other services registered earlier in
        /// the same sequence are safe to look up on <paramref name="registry"/> here; services
        /// registered later are not yet initialized and <see cref="IServiceRegistry.Get{TService}"/>
        /// will throw for them.
        /// </summary>
        void Initialize(IServiceRegistry registry);

        /// <summary>
        /// Called once by the bootstrap during shutdown. Must leave the service safe to be
        /// garbage collected; do not assume other services are still initialized at this point.
        /// </summary>
        void Shutdown();
    }
}
