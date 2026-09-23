using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;

namespace GameFramework.RemoteConfig.Tests
{
    /// <summary>See <c>GameFramework.Monetization.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out EventService events, out PersistenceService persistence, out TimerService timer)
        {
            return Build(new InMemoryPersistenceStorage(), out events, out persistence, out timer);
        }

        /// <summary>Builds a second, otherwise-independent set of services sharing the same backing
        /// <paramref name="storage"/> - simulates an application restart (a fresh
        /// <see cref="RemoteConfigService"/> instance with no in-memory state, reading whatever a
        /// previous instance actually persisted to cache).</summary>
        public static ServiceRegistry Build(InMemoryPersistenceStorage storage, out EventService events, out PersistenceService persistence, out TimerService timer)
        {
            var registry = new ServiceRegistry();

            var time = new FakeTimeService();
            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            registry.Register<IPersistenceService>(persistence);
            persistence.Initialize(registry);
            registry.MarkInitialized(typeof(IPersistenceService));

            timer = new TimerService();
            registry.Register<ITimerService>(timer);
            timer.Initialize(registry);
            registry.MarkInitialized(typeof(ITimerService));

            return registry;
        }
    }
}
