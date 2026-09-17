using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Rewards.Tests
{
    /// <summary>See <c>GameFramework.Progression.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly since these are test-only internal helpers.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out EventService events, out PersistenceService persistence)
        {
            var registry = new ServiceRegistry();

            var time = new FakeTimeService();
            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            persistence = new PersistenceService(new InMemoryPersistenceStorage(), new JsonPersistenceSerializer());
            registry.Register<IPersistenceService>(persistence);
            persistence.Initialize(registry);
            registry.MarkInitialized(typeof(IPersistenceService));

            return registry;
        }
    }
}
