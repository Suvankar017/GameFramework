using GameFramework.Monetization.Entitlements;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;

namespace GameFramework.Monetization.Tests
{
    /// <summary>See <c>GameFramework.Rewards.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly. Additionally registers a real
    /// <see cref="RewardService"/>/<see cref="EntitlementService"/> (rather than only constructing
    /// them unregistered) because <c>Purchases.PurchaseService</c>/<c>Ads.AdsService</c> resolve
    /// both via <c>IServiceRegistry.TryGet</c>, which only succeeds once a service is registered
    /// <i>and</i> marked initialized.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(
            out FakeTimeService time,
            out EventService events,
            out PersistenceService persistence,
            out TimerService timer,
            out RewardService rewards,
            out EntitlementService entitlements)
        {
            return Build(new InMemoryPersistenceStorage(), out time, out events, out persistence, out timer, out rewards, out entitlements);
        }

        /// <summary>Builds a second, otherwise-independent set of services sharing the same backing
        /// <paramref name="storage"/> - simulates an application restart (a fresh
        /// <see cref="EntitlementService"/>/<see cref="RewardService"/> instance with no in-memory
        /// state, reading whatever a previous instance actually persisted).</summary>
        public static ServiceRegistry Build(
            InMemoryPersistenceStorage storage,
            out FakeTimeService time,
            out EventService events,
            out PersistenceService persistence,
            out TimerService timer,
            out RewardService rewards,
            out EntitlementService entitlements)
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

            entitlements = new EntitlementService();
            registry.Register<IEntitlementService>(entitlements);
            entitlements.Initialize(registry);
            registry.MarkInitialized(typeof(IEntitlementService));

            rewards = new RewardService();
            registry.Register<IRewardService>(rewards);
            rewards.Initialize(registry);
            registry.MarkInitialized(typeof(IRewardService));

            return registry;
        }
    }
}
