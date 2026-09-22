using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Purchases;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Monetization.Tests
{
    public class PurchaseServiceTests
    {
        private static readonly ProductId CoinsProduct = new ProductId("coins_100");
        private static readonly ProductId RemoveAdsProduct = new ProductId("remove_ads");
        private static readonly EntitlementId RemoveAdsEntitlement = new EntitlementId("remove_ads");
        private static readonly RewardId CoinsRewardId = new RewardId("CoinsReward");

        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private EntitlementService _entitlements;
        private RewardService _rewards;
        private RewardDefinition _coinsRewardDefinition;
        private CountingReward _coinsReward;
        private ProductCatalog _catalog;
        private FakePurchaseProvider _provider;
        private PurchaseService _purchases;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out FakeTimeService _, out _events, out _persistence, out TimerService _, out _rewards, out _entitlements);

            _coinsRewardDefinition = TestDefinitions.Reward("CoinsReward", RewardClaimPolicy.Repeatable);
            _coinsReward = new CountingReward();
            _rewards.RegisterReward(_coinsRewardDefinition, _coinsReward);

            _catalog = TestDefinitions.Catalog(
                TestDefinitions.Product(CoinsProduct.Value, ProductType.Consumable, rewardId: CoinsRewardId.Value),
                TestDefinitions.Product(RemoveAdsProduct.Value, ProductType.NonConsumable, entitlementId: RemoveAdsEntitlement.Value));

            _provider = new FakePurchaseProvider();
            _purchases = new PurchaseService(_catalog, _provider);
            _purchases.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _purchases.Shutdown();
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_coinsRewardDefinition);
        }

        [Test]
        public void Initialize_SetsStateInitialized()
        {
            Assert.AreEqual(MonetizationProviderState.Initialized, _purchases.State);
        }

        [Test]
        public void Purchase_UnknownProduct_ReturnsProductUnavailable()
        {
            PurchaseResult? result = null;
            _purchases.Purchase(new ProductId("nope"), r => result = r);

            Assert.AreEqual(PurchaseResultKind.ProductUnavailable, result.Value.Kind);
            Assert.IsEmpty(_provider.PurchaseCalls);
        }

        [Test]
        public void Purchase_Success_GrantsEntitlement()
        {
            PurchaseResult? result = null;
            _purchases.Purchase(RemoveAdsProduct, r => result = r);

            Assert.AreEqual(PurchaseResultKind.Success, result.Value.Kind);
            Assert.IsTrue(_entitlements.HasEntitlement(RemoveAdsEntitlement));
        }

        [Test]
        public void Purchase_Success_PublishesPurchaseCompletedEvent()
        {
            bool published = false;
            _events.Subscribe<PurchaseCompletedEvent>(_ => published = true);

            _purchases.Purchase(RemoveAdsProduct, _ => { });

            Assert.IsTrue(published);
        }

        [Test]
        public void Purchase_Consumable_ClaimsReward()
        {
            _purchases.Purchase(CoinsProduct, _ => { });

            Assert.AreEqual(1, _coinsReward.GrantCount);
        }

        [Test]
        public void Purchase_AlreadyOwnedNonConsumable_SkipsProvider()
        {
            _entitlements.GrantEntitlement(RemoveAdsEntitlement, EntitlementSource.Purchase);

            PurchaseResult? result = null;
            _purchases.Purchase(RemoveAdsProduct, r => result = r);

            Assert.AreEqual(PurchaseResultKind.AlreadyOwned, result.Value.Kind);
            Assert.IsEmpty(_provider.PurchaseCalls);
        }

        [Test]
        public void Purchase_DuplicateTransactionId_DoesNotDoubleGrantReward()
        {
            _provider.NextPurchaseResults.Enqueue(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "fixed-txn", null));
            _purchases.Purchase(CoinsProduct, _ => { });
            Assert.AreEqual(1, _coinsReward.GrantCount);

            // Same transaction id delivered again (a duplicate provider callback) must not re-grant.
            _provider.RaisePurchaseUpdated(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "fixed-txn", null));

            Assert.AreEqual(1, _coinsReward.GrantCount, "Duplicate transaction callback must not grant twice.");
        }

        [Test]
        public void Purchase_ValidationFailed_WhenTransactionIdMissing_DoesNotGrant()
        {
            _provider.NextPurchaseResults.Enqueue(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, string.Empty, null));

            PurchaseResult? result = null;
            bool failedEvent = false;
            _events.Subscribe<PurchaseFailedEvent>(_ => failedEvent = true);

            _purchases.Purchase(CoinsProduct, r => result = r);

            Assert.AreEqual(PurchaseResultKind.ValidationFailed, result.Value.Kind);
            Assert.IsTrue(failedEvent);
            Assert.AreEqual(0, _coinsReward.GrantCount);
        }

        [Test]
        public void Purchase_Cancelled_FiresPurchaseFailed_NoGrant()
        {
            _provider.NextPurchaseResults.Enqueue(PurchaseResult.Immediate(PurchaseResultKind.Cancelled, CoinsProduct));

            bool failedEvent = false;
            _events.Subscribe<PurchaseFailedEvent>(_ => failedEvent = true);

            PurchaseResult? result = null;
            _purchases.Purchase(CoinsProduct, r => result = r);

            Assert.AreEqual(PurchaseResultKind.Cancelled, result.Value.Kind);
            Assert.IsTrue(failedEvent);
            Assert.AreEqual(0, _coinsReward.GrantCount);
        }

        [Test]
        public void Purchase_Pending_ThenProviderUpdateResolves_InvokesOriginalCallbackAgain()
        {
            _provider.NextPurchaseResults.Enqueue(PurchaseResult.Immediate(PurchaseResultKind.Pending, CoinsProduct));

            int callbackCount = 0;
            PurchaseResult? lastResult = null;
            _purchases.Purchase(CoinsProduct, r => { callbackCount++; lastResult = r; });

            Assert.AreEqual(1, callbackCount);
            Assert.AreEqual(PurchaseResultKind.Pending, lastResult.Value.Kind);
            Assert.AreEqual(0, _coinsReward.GrantCount);

            _provider.RaisePurchaseUpdated(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "deferred-txn", null));

            Assert.AreEqual(2, callbackCount, "The original caller should be notified once the pending purchase resolves.");
            Assert.AreEqual(PurchaseResultKind.Success, lastResult.Value.Kind);
            Assert.AreEqual(1, _coinsReward.GrantCount);
        }

        [Test]
        public void RestorePurchases_GrantsEntitlementForRestoredProduct()
        {
            _provider.NextRestoreResult = new RestoreResult(true, new[] { RemoveAdsProduct }, null);

            RestoreResult? result = null;
            _purchases.RestorePurchases(r => result = r);

            Assert.IsTrue(result.Value.Success);
            Assert.IsTrue(_entitlements.HasEntitlement(RemoveAdsEntitlement));
        }

        [Test]
        public void RestorePurchases_CalledTwice_DoesNotDoubleGrant()
        {
            _provider.NextRestoreResult = new RestoreResult(true, new[] { RemoveAdsProduct }, null);

            _purchases.RestorePurchases(_ => { });
            _entitlements.RevokeEntitlement(RemoveAdsEntitlement); // simulate the game itself revoking it
            _purchases.RestorePurchases(_ => { });

            // The dedupe key ("restore:remove_ads") was already processed by the first restore, so
            // the second restore does not re-grant even though the entitlement was revoked in between.
            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAdsEntitlement));
        }

        [Test]
        public void IsTransactionProcessed_ReflectsGrantedPurchases()
        {
            _provider.NextPurchaseResults.Enqueue(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "txn-1", null));
            _purchases.Purchase(CoinsProduct, _ => { });

            Assert.IsTrue(_purchases.IsTransactionProcessed("txn-1"));
            Assert.IsFalse(_purchases.IsTransactionProcessed("unknown-txn"));
        }
    }
}
