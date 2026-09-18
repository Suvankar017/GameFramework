using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;

namespace GameFramework.Presentation.Tests
{
    /// <summary>See <c>GameFramework.Tutorials.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly. Builds only the four hard dependencies every
    /// <see cref="PresentationService"/> test needs (Time/Events/Settings/Timers) - a test that
    /// needs Audio/Feedback/UI/a camera driver registers those fakes itself, on this same registry,
    /// before calling <see cref="PresentationService.Initialize"/>.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out FakeTimeService time, out EventService events, out SettingsService settings, out TimerService timers)
        {
            var registry = new ServiceRegistry();

            time = new FakeTimeService();
            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));

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

            timers = new TimerService();
            registry.Register<ITimerService>(timers);
            timers.Initialize(registry);
            registry.MarkInitialized(typeof(ITimerService));

            return registry;
        }
    }
}
