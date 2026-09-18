using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.GameFlow.Tests
{
    /// <summary>See <c>GameFramework.Quests.Tests.TestRegistryFactory</c>'s remarks - same pattern,
    /// duplicated per test assembly since these are test-only internal helpers.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out FakeTimeService time, out FakeSceneService scene, out EventService events, out PersistenceService persistence)
        {
            var registry = new ServiceRegistry();

            time = new FakeTimeService();
            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));

            scene = new FakeSceneService();
            registry.Register<ISceneService>(scene);
            registry.MarkInitialized(typeof(ISceneService));

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
