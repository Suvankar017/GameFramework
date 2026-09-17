using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Progression.Tests
{
    /// <summary>
    /// Builds a minimal, already-initialized <see cref="ServiceRegistry"/> for testing a Phase 6
    /// service in isolation - a fake <see cref="ITimeService"/>, plus the real
    /// <see cref="EventService"/> and a real <see cref="PersistenceService"/> backed by
    /// <see cref="InMemoryPersistenceStorage"/> (isolated, deterministic storage, never the
    /// developer's real save files) - the same "register a dependency, MarkInitialized it" pattern
    /// every previous phase's tests already established.
    /// </summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out FakeTimeService time, out EventService events, out PersistenceService persistence)
        {
            var registry = new ServiceRegistry();

            time = new FakeTimeService();
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
