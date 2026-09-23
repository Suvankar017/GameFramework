using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;

namespace GameFramework.DeepLinks.Tests
{
    /// <summary>See <c>Monetization.Tests.TestRegistryFactory</c>'s remarks - same pattern,
    /// duplicated per test assembly. <see cref="DeepLinkService"/> only requires
    /// <see cref="IEventService"/>.</summary>
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
