using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;

namespace GameFramework.Notifications.Tests
{
    /// <summary>See <c>Monetization.Tests.TestRegistryFactory</c>'s remarks - same pattern,
    /// duplicated per test assembly. <see cref="NotificationService"/>/<see cref="DeepLinks.DeepLinkService"/>
    /// only require <see cref="IEventService"/>; <see cref="Localization.ILocalizationService"/> is
    /// registered separately (only where a test actually needs localized content resolution) via
    /// <see cref="FakeLocalizationService"/>.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(out EventService events)
        {
            var registry = new ServiceRegistry();

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            return registry;
        }
    }
}
