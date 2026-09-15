namespace GameFramework.Runtime.Services
{
    /// <summary>
    /// Type-keyed lookup for application-level services. One instance per service type; the type
    /// registered against is typically an interface (e.g. register against <c>ILoggingService</c>,
    /// not the concrete <c>LoggingService</c>) so consumers depend on the abstraction.
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// Registers <paramref name="service"/> against <typeparamref name="TService"/>. Throws if
        /// a service of that type is already registered. Registering does not initialize the
        /// service — that is the bootstrap's responsibility.
        /// </summary>
        void Register<TService>(TService service) where TService : class;

        /// <summary>
        /// Returns the registered, initialized instance of <typeparamref name="TService"/>.
        /// Throws <see cref="ServiceNotFoundException"/> if none is registered, or
        /// <see cref="System.InvalidOperationException"/> if it is registered but not yet
        /// initialized.
        /// </summary>
        TService Get<TService>() where TService : class;

        /// <summary>
        /// Non-throwing variant of <see cref="Get{TService}"/>. Returns false if the service is
        /// missing or not yet initialized.
        /// </summary>
        bool TryGet<TService>(out TService service) where TService : class;

        /// <summary>
        /// True if a service is registered for <typeparamref name="TService"/>, regardless of
        /// whether it has been initialized yet.
        /// </summary>
        bool IsRegistered<TService>() where TService : class;
    }
}
