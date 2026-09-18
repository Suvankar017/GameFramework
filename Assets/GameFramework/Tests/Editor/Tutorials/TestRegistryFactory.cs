using GameFramework.Input;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>See <c>GameFramework.GameFlow.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly since these are test-only internal helpers.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out FakeTimeService time, out FakeInputService input, out EventService events, out PersistenceService persistence)
        {
            var registry = new ServiceRegistry();

            time = new FakeTimeService();
            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));

            input = new FakeInputService();
            registry.Register<IInputService>(input);
            registry.MarkInitialized(typeof(IInputService));

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
