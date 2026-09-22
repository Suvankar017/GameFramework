using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Analytics.Tests
{
    /// <summary>See <c>GameFramework.Monetization.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly. <see cref="AnalyticsService"/>/<see cref="Diagnostics.DiagnosticsService"/>
    /// only require <see cref="IEventService"/>/<see cref="IPersistenceService"/> to already be
    /// registered and initialized (both softly resolve everything else - see those services'
    /// remarks), so this factory is deliberately smaller than Monetization's.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out EventService events, out PersistenceService persistence) =>
            Build(new InMemoryPersistenceStorage(), out events, out persistence);

        public static ServiceRegistry Build(InMemoryPersistenceStorage storage, out EventService events, out PersistenceService persistence)
        {
            var registry = new ServiceRegistry();

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            registry.Register<IPersistenceService>(persistence);
            persistence.Initialize(registry);
            registry.MarkInitialized(typeof(IPersistenceService));

            return registry;
        }
    }
}
