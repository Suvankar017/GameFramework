using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;

namespace GameFramework.PlayerData.Tests
{
    /// <summary>See <c>GameFramework.Rewards.Tests.TestRegistryFactory</c>'s remarks - same pattern,
    /// duplicated per test assembly since these are test-only internal helpers. Includes
    /// <see cref="ITimerService"/> (backed by a controllable <see cref="FakeTimeService"/>) since
    /// <see cref="PlayerProfileService"/>'s debounced autosave needs it.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(
            out FakeTimeService time,
            out EventService events,
            out PersistenceService persistence,
            out TimerService timer,
            out PlayerProfileService playerData)
        {
            return Build(out time, out events, out persistence, out timer, out playerData, out _);
        }

        public static ServiceRegistry Build(
            out FakeTimeService time,
            out EventService events,
            out PersistenceService persistence,
            out TimerService timer,
            out PlayerProfileService playerData,
            out InMemoryPersistenceStorage storage)
        {
            storage = new InMemoryPersistenceStorage();
            return BuildWithStorage(storage, out time, out events, out persistence, out timer, out playerData);
        }

        /// <summary>Builds a second, otherwise-independent set of services sharing the same
        /// backing <paramref name="storage"/> - simulates an application restart (a fresh
        /// <see cref="PlayerProfileService"/> instance with no in-memory state, reading whatever a
        /// previous instance actually persisted) - see <c>CorruptionAndBackupTests</c>.</summary>
        public static ServiceRegistry BuildWithStorage(
            InMemoryPersistenceStorage storage,
            out FakeTimeService time,
            out EventService events,
            out PersistenceService persistence,
            out TimerService timer,
            out PlayerProfileService playerData)
        {
            var registry = new ServiceRegistry();

            time = new FakeTimeService();
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

            playerData = new PlayerProfileService();
            registry.Register<IPlayerProfileService>(playerData);
            playerData.Initialize(registry);
            registry.MarkInitialized(typeof(IPlayerProfileService));

            return registry;
        }
    }
}
