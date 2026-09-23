using System;
using System.Collections.Generic;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Providers;
using GameFramework.Monetization.Providers.Mock;
using GameFramework.Monetization.Purchases;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Security;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameFramework.Monetization.Tests
{
    /// <summary>Phase 19: provider-result validation, provider failure isolation, entitlement
    /// load validation, the stored-vs-verified entitlement distinction, and mock provider isolation.</summary>
    public class MonetizationHardeningTests
    {
        private static readonly ProductId RemoveAdsProduct = new ProductId("remove_ads");
        private static readonly ProductId CoinsProduct = new ProductId("coins_100");
        private static readonly EntitlementId RemoveAdsEntitlement = new EntitlementId("remove_ads");

        private sealed class ThrowingPurchaseProvider : IPurchaseProvider
        {
            public bool ThrowOnInitialize;
            public bool IsInitialized => true;

            public void Initialize(IReadOnlyList<ProductDefinition> products, Action<bool> onComplete)
            {
                if (ThrowOnInitialize)
                {
                    throw new InvalidOperationException("SDK crashed");
                }

                onComplete(true);
            }

            public bool TryGetProduct(ProductId id, out Product product)
            {
                product = default;
                return false;
            }

            public IReadOnlyList<Product> GetProducts() => Array.Empty<Product>();
            public void Purchase(ProductId id, Action<PurchaseResult> onComplete) => throw new InvalidOperationException("SDK crashed");
            public void RestorePurchases(Action<RestoreResult> onComplete) => throw new InvalidOperationException("SDK crashed");
#pragma warning disable 0067
            public event Action<PurchaseResult> PurchaseUpdated;
#pragma warning restore 0067
        }

        private InMemoryPersistenceStorage _storage;
        private ServiceRegistry _registry;
        private PersistenceService _persistence;
        private EntitlementService _entitlements;
        private ProductCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _registry = TestRegistryFactory.Build(_storage, out FakeTimeService _, out EventService _, out _persistence, out TimerService _, out RewardService _, out _entitlements);
            _catalog = TestDefinitions.Catalog(
                TestDefinitions.Product(RemoveAdsProduct.Value, ProductType.NonConsumable, entitlementId: RemoveAdsEntitlement.Value),
                TestDefinitions.Product(CoinsProduct.Value, ProductType.Consumable));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Validator_ResultForDifferentProduct_IsInvalid()
        {
            var validator = new LocalPurchaseValidator();
            ProductDefinition removeAds = _catalog.Products[0];
            var result = new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "txn-1", null);

            Assert.IsFalse(validator.Validate(result, removeAds).IsValid);
        }

        [Test]
        public void Validator_OversizedTransactionId_IsInvalid()
        {
            var validator = new LocalPurchaseValidator();
            var result = new PurchaseResult(PurchaseResultKind.Success, RemoveAdsProduct,
                new string('x', LocalPurchaseValidator.MaxTransactionIdLength + 1), null);

            Assert.IsFalse(validator.Validate(result, _catalog.Products[0]).IsValid);
        }

        [Test]
        public void Purchase_ProviderReportsMismatchedProduct_GrantsNothing()
        {
            var provider = new FakePurchaseProvider();
            provider.NextPurchaseResults.Enqueue(new PurchaseResult(PurchaseResultKind.Success, CoinsProduct, "txn-1", null));
            var purchases = new PurchaseService(_catalog, provider);
            purchases.Initialize(_registry);

            PurchaseResult reported = default;
            purchases.Purchase(RemoveAdsProduct, r => reported = r);

            Assert.AreEqual(PurchaseResultKind.ValidationFailed, reported.Kind);
            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAdsEntitlement));
            purchases.Shutdown();
        }

        [Test]
        public void Restore_UnknownProduct_IsIgnoredNotGranted()
        {
            var provider = new FakePurchaseProvider
            {
                NextRestoreResult = new RestoreResult(true, new[] { new ProductId("not_in_catalog") }, null)
            };
            var purchases = new PurchaseService(_catalog, provider);
            purchases.Initialize(_registry);

            purchases.RestorePurchases(null);

            Assert.AreEqual(0, _entitlements.OwnedEntitlements.Count);
            purchases.Shutdown();
        }

        [Test]
        public void Provider_ThrowsOnInitialize_ServiceSettlesFailedWithoutThrowing()
        {
            var purchases = new PurchaseService(_catalog, new ThrowingPurchaseProvider { ThrowOnInitialize = true });

            Assert.DoesNotThrow(() => purchases.Initialize(_registry));
            Assert.AreEqual(MonetizationProviderState.Failed, purchases.State);
            purchases.Shutdown();
        }

        [Test]
        public void Provider_ThrowsOnPurchaseAndRestore_ReportedAsFailures()
        {
            var purchases = new PurchaseService(_catalog, new ThrowingPurchaseProvider());
            purchases.Initialize(_registry);

            PurchaseResult purchaseResult = default;
            RestoreResult restoreResult = default;
            Assert.DoesNotThrow(() => purchases.Purchase(RemoveAdsProduct, r => purchaseResult = r));
            Assert.DoesNotThrow(() => purchases.RestorePurchases(r => restoreResult = r));

            Assert.AreEqual(PurchaseResultKind.Failed, purchaseResult.Kind);
            Assert.IsFalse(restoreResult.Success);
            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAdsEntitlement));
            purchases.Shutdown();
        }

        [Test]
        public void EntitlementLoad_OutOfRangeTicksAndUnknownSource_DoNotThrowAndTreatAsExpired()
        {
            var data = new EntitlementSaveData();
            data.Entries.Add(new EntitlementSaveData.Entry { Id = "vip", IsOwned = true, ExpirationUtcTicks = long.MaxValue, Source = 99 });
            _persistence.Save("GameFramework.Monetization.Entitlements", data, 1);

            var reloaded = new EntitlementService();
            Assert.DoesNotThrow(() => reloaded.Initialize(_registry));

            EntitlementState state = reloaded.GetEntitlement(new EntitlementId("vip"));
            Assert.IsFalse(reloaded.HasEntitlement(new EntitlementId("vip")), "An unrepresentable expiration must never mean 'forever'.");
            Assert.AreEqual(EntitlementSource.Granted, state.Source);
        }

        [Test]
        public void Entitlement_LoadedFromDisk_IsNotVerifiedThisSession()
        {
            _entitlements.GrantEntitlement(RemoveAdsEntitlement, EntitlementSource.Purchase);
            _entitlements.Save();

            var reloaded = new EntitlementService();
            reloaded.Initialize(_registry);

            Assert.IsTrue(reloaded.HasEntitlement(RemoveAdsEntitlement), "The local cache still answers HasEntitlement.");
            Assert.IsFalse(reloaded.IsVerifiedThisSession(RemoveAdsEntitlement), "Local data alone is not provider verification.");
        }

        [Test]
        public void Entitlement_FromCompletedPurchase_IsVerifiedThisSession()
        {
            var purchases = new PurchaseService(_catalog, new FakePurchaseProvider());
            purchases.Initialize(_registry);

            purchases.Purchase(RemoveAdsProduct, null);

            Assert.IsTrue(_entitlements.IsVerifiedThisSession(RemoveAdsEntitlement));
            purchases.Shutdown();
        }

        [Test]
        public void Entitlement_ManualGrant_IsNotVerified()
        {
            _entitlements.GrantEntitlement(new EntitlementId("promo"), EntitlementSource.Granted);

            Assert.IsFalse(_entitlements.IsVerifiedThisSession(new EntitlementId("promo")));
        }

        [Test]
        public void NoOpPurchaseProvider_NeverGrants()
        {
            var purchases = new PurchaseService(_catalog, new NoOpPurchaseProvider());
            purchases.Initialize(_registry);

            PurchaseResult result = default;
            purchases.Purchase(RemoveAdsProduct, r => result = r);

            Assert.AreEqual(MonetizationProviderState.Failed, purchases.State);
            Assert.AreEqual(PurchaseResultKind.NotInitialized, result.Kind);
            Assert.IsFalse(_entitlements.HasEntitlement(RemoveAdsEntitlement));
            purchases.Shutdown();
        }

        [Test]
        public void NoOpAdProvider_ReportsNoAdsAvailable()
        {
            var provider = new NoOpAdProvider();
            bool? initialized = null;
            provider.Initialize(success => initialized = success);

            Assert.AreEqual(false, initialized);
            Assert.IsFalse(provider.IsAdAvailable(new AdPlacementId("any"), AdType.Rewarded));
        }

        [Test]
        public void Guard_ReleaseBuild_ReplacesMockPurchaseProviderWithNoOp()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("development-only"));

            IPurchaseProvider provider = DevelopmentProviderGuard.Select<IPurchaseProvider>(
                true, false, () => new MockPurchaseProvider(MockPurchaseSimulationMode.AlwaysSucceed), () => new NoOpPurchaseProvider(), "Test");

            Assert.IsInstanceOf<NoOpPurchaseProvider>(provider);
        }
    }
}
