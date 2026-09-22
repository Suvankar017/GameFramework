using System;
using System.Collections.Generic;
using GameFramework.Monetization.Entitlements;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Rewards;
using GameFramework.Runtime.Timers;
using NUnit.Framework;

namespace GameFramework.Monetization.Tests
{
    public class EntitlementServiceTests
    {
        private static readonly EntitlementId RemoveAds = new EntitlementId("remove_ads");
        private static readonly EntitlementId Premium = new EntitlementId("premium");

        private EventService _events;
        private PersistenceService _persistence;
        private EntitlementService _entitlements;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out FakeTimeService _, out _events, out _persistence, out TimerService _, out RewardService _, out _entitlements);
        }

        [Test]
        public void GetEntitlement_Unknown_ReturnsNotOwned()
        {
            EntitlementState state = _entitlements.GetEntitlement(RemoveAds);

            Assert.IsFalse(state.IsOwned);
            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAds));
        }

        [Test]
        public void GrantEntitlement_MarksOwned()
        {
            _entitlements.GrantEntitlement(RemoveAds, EntitlementSource.Purchase);

            Assert.IsTrue(_entitlements.HasEntitlement(RemoveAds));
            Assert.Contains(RemoveAds, new List<EntitlementId>(_entitlements.OwnedEntitlements));
        }

        [Test]
        public void GrantEntitlement_PublishesChangedEvent_AndCSharpEvent()
        {
            bool eventPublished = false;
            _events.Subscribe<EntitlementChangedEvent>(evt => eventPublished = evt.IsOwned);

            bool csharpEventFired = false;
            _entitlements.EntitlementChanged += (id, owned) => csharpEventFired = owned;

            _entitlements.GrantEntitlement(RemoveAds, EntitlementSource.Purchase);

            Assert.IsTrue(eventPublished);
            Assert.IsTrue(csharpEventFired);
        }

        [Test]
        public void RevokeEntitlement_RemovesOwnership()
        {
            _entitlements.GrantEntitlement(RemoveAds, EntitlementSource.Purchase);
            _entitlements.RevokeEntitlement(RemoveAds);

            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAds));
        }

        [Test]
        public void RevokeEntitlement_NotOwned_IsNoOp()
        {
            _entitlements.RevokeEntitlement(RemoveAds); // never granted

            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAds));
        }

        [Test]
        public void Expiration_InThePast_HasEntitlementFalse_ButStillOnRecord()
        {
            _entitlements.GrantEntitlement(Premium, EntitlementSource.Purchase, DateTime.UtcNow.AddDays(-1));

            Assert.IsFalse(_entitlements.HasEntitlement(Premium));
            Assert.IsTrue(_entitlements.GetEntitlement(Premium).IsOwned, "Expired is not the same as never owned.");
        }

        [Test]
        public void Expiration_InTheFuture_HasEntitlementTrue()
        {
            _entitlements.GrantEntitlement(Premium, EntitlementSource.Purchase, DateTime.UtcNow.AddDays(30));

            Assert.IsTrue(_entitlements.HasEntitlement(Premium));
        }

        [Test]
        public void SyncFromProvider_GrantsWithRestoredSource()
        {
            var restored = new[] { new EntitlementState(RemoveAds, true, null, false, EntitlementSource.Purchase) };

            _entitlements.SyncFromProvider(restored);

            Assert.IsTrue(_entitlements.HasEntitlement(RemoveAds));
            Assert.AreEqual(EntitlementSource.Restored, _entitlements.GetEntitlement(RemoveAds).Source);
        }

        [Test]
        public void SaveLoad_RoundTripsAcrossInstances_SharingStorage()
        {
            var storage = new InMemoryPersistenceStorage();
            TestRegistryFactory.Build(storage, out FakeTimeService _, out EventService _, out PersistenceService _,
                out TimerService _, out RewardService _, out EntitlementService firstInstance);

            firstInstance.GrantEntitlement(RemoveAds, EntitlementSource.Purchase);
            firstInstance.GrantEntitlement(Premium, EntitlementSource.Purchase, DateTime.UtcNow.AddDays(10), isAutoRenewing: true);
            firstInstance.Save();

            // A fresh instance over the same storage - simulates an application restart.
            TestRegistryFactory.Build(storage, out FakeTimeService _, out EventService _, out PersistenceService _,
                out TimerService _, out RewardService _, out EntitlementService secondInstance);

            Assert.IsTrue(secondInstance.HasEntitlement(RemoveAds));
            EntitlementState premiumState = secondInstance.GetEntitlement(Premium);
            Assert.IsTrue(premiumState.IsOwned);
            Assert.IsTrue(premiumState.IsAutoRenewing);
            Assert.IsTrue(premiumState.ExpirationUtc.HasValue);
        }
    }
}
