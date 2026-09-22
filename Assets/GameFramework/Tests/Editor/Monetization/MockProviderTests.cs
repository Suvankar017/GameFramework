using System.Collections.Generic;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Providers.Mock;
using GameFramework.Monetization.Purchases;
using NUnit.Framework;

namespace GameFramework.Monetization.Tests
{
    /// <summary>Deterministic-behavior coverage for the framework's shipped mock providers - see
    /// CLAUDE.md's Phase 15 brief, section 39/40. These exercise the mocks directly, distinct from
    /// <c>AdsServiceTests</c>/<c>PurchaseServiceTests</c>, which use the fully-controllable
    /// <c>FakeAdProvider</c>/<c>FakePurchaseProvider</c> to unit-test orchestration in isolation.</summary>
    public class MockProviderTests
    {
        [Test]
        public void MockAdProvider_AlwaysSucceed_LoadsAndShowsInterstitial()
        {
            var provider = new MockAdProvider(MockAdSimulationMode.AlwaysSucceed);
            var id = new AdPlacementId("Interstitial");
            bool loaded = false, shown = false, closed = false;
            provider.AdLoaded += _ => loaded = true;
            provider.AdShown += _ => shown = true;
            provider.AdClosed += _ => closed = true;

            provider.Load(id, AdType.Interstitial, string.Empty);
            provider.Show(id, AdType.Interstitial);

            Assert.IsTrue(loaded);
            Assert.IsTrue(shown);
            Assert.IsTrue(closed);
        }

        [Test]
        public void MockAdProvider_AlwaysSucceed_RewardedEarnsRewardBeforeClosing()
        {
            var provider = new MockAdProvider(MockAdSimulationMode.AlwaysSucceed);
            var id = new AdPlacementId("Rewarded");
            var order = new List<string>();
            provider.AdRewardEarned += _ => order.Add("reward");
            provider.AdClosed += _ => order.Add("closed");

            provider.Load(id, AdType.Rewarded, string.Empty);
            provider.Show(id, AdType.Rewarded);

            CollectionAssert.AreEqual(new[] { "reward", "closed" }, order);
        }

        [Test]
        public void MockAdProvider_RewardedClosesWithoutReward_NeverRaisesRewardEarned()
        {
            var provider = new MockAdProvider(MockAdSimulationMode.RewardedClosesWithoutReward);
            var id = new AdPlacementId("Rewarded");
            bool rewardEarned = false, closed = false;
            provider.AdRewardEarned += _ => rewardEarned = true;
            provider.AdClosed += _ => closed = true;

            provider.Load(id, AdType.Rewarded, string.Empty);
            provider.Show(id, AdType.Rewarded);

            Assert.IsFalse(rewardEarned);
            Assert.IsTrue(closed);
        }

        [Test]
        public void MockAdProvider_AlwaysFailToLoad_NeverBecomesAvailable()
        {
            var provider = new MockAdProvider(MockAdSimulationMode.AlwaysFailToLoad);
            var id = new AdPlacementId("Interstitial");
            bool failed = false;
            provider.AdLoadFailed += (_, __) => failed = true;

            provider.Load(id, AdType.Interstitial, string.Empty);

            Assert.IsTrue(failed);
            Assert.IsFalse(provider.IsAdAvailable(id, AdType.Interstitial));
        }

        [Test]
        public void MockAdProvider_Banner_StaysShownUntilHide()
        {
            var provider = new MockAdProvider(MockAdSimulationMode.AlwaysSucceed);
            var id = new AdPlacementId("Banner");
            bool closed = false;
            provider.AdClosed += _ => closed = true;

            provider.Load(id, AdType.Banner, string.Empty);
            provider.Show(id, AdType.Banner);

            Assert.IsFalse(closed, "A banner must not auto-close like an Interstitial/Rewarded ad.");
        }

        [Test]
        public void MockPurchaseProvider_AlwaysSucceed_ReturnsSuccessWithTransactionId()
        {
            var provider = new MockPurchaseProvider(MockPurchaseSimulationMode.AlwaysSucceed);
            var definition = TestDefinitions.Product("coins_100", ProductType.Consumable);
            provider.Initialize(new[] { definition }, _ => { });

            PurchaseResult? result = null;
            provider.Purchase(new ProductId("coins_100"), r => result = r);

            Assert.AreEqual(PurchaseResultKind.Success, result.Value.Kind);
            Assert.IsNotEmpty(result.Value.TransactionId);
        }

        [Test]
        public void MockPurchaseProvider_RestorePurchases_ReturnsPreviouslyPurchasedNonConsumables()
        {
            var provider = new MockPurchaseProvider(MockPurchaseSimulationMode.AlwaysSucceed);
            var definition = TestDefinitions.Product("remove_ads", ProductType.NonConsumable);
            provider.Initialize(new[] { definition }, _ => { });
            provider.Purchase(new ProductId("remove_ads"), _ => { });

            RestoreResult? restoreResult = null;
            provider.RestorePurchases(r => restoreResult = r);

            Assert.IsTrue(restoreResult.Value.Success);
            CollectionAssert.Contains(restoreResult.Value.RestoredProductIds, new ProductId("remove_ads"));
        }

        [Test]
        public void MockPurchaseProvider_AlwaysCancel_ReturnsCancelled()
        {
            var provider = new MockPurchaseProvider(MockPurchaseSimulationMode.AlwaysCancel);
            var definition = TestDefinitions.Product("coins_100", ProductType.Consumable);
            provider.Initialize(new[] { definition }, _ => { });

            PurchaseResult? result = null;
            provider.Purchase(new ProductId("coins_100"), r => result = r);

            Assert.AreEqual(PurchaseResultKind.Cancelled, result.Value.Kind);
        }
    }
}
