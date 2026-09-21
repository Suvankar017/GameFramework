using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;

namespace GameFramework.Cameras.Tests
{
    /// <summary>See <c>Presentation.Tests.TestRegistryFactory</c>'s remarks - same pattern,
    /// duplicated per test assembly. Builds the two dependencies <see cref="CameraService"/> uses
    /// (Events required, Settings soft) - a test that needs Settings-driven behavior (the
    /// "Camera.ReduceMotion" accessibility setting) reads it off the returned registry.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out EventService events, out SettingsService settings)
        {
            var registry = new ServiceRegistry();

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            var persistence = new PersistenceService(new InMemoryPersistenceStorage(), new JsonPersistenceSerializer());
            registry.Register<IPersistenceService>(persistence);
            persistence.Initialize(registry);
            registry.MarkInitialized(typeof(IPersistenceService));

            settings = new SettingsService();
            registry.Register<ISettingsService>(settings);
            settings.Initialize(registry);
            registry.MarkInitialized(typeof(ISettingsService));

            return registry;
        }
    }
}
