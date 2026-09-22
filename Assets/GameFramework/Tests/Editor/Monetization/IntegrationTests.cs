using System.Collections.Generic;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Integration;
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
    /// <summary>End-to-end flows spanning Ads/Purchases/Entitlements/Rewards - see CLAUDE.md's
    /// Phase 15 brief, section 40 ("Integration" test scenarios).</summary>
    public class IntegrationTests
    {
        [Test]
        public void RemoveAdsPurchase_SuppressesBannerAndInterstitial_ButNotRewarded()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(
                out FakeTimeService _, out EventService events, out PersistenceService _, out TimerService _,
                out RewardService rewards, out EntitlementService entitlements);

            var catalog = TestDefinitions.Catalog(
                TestDefinitions.Product("remove_ads", ProductType.NonConsumable, entitlementId: "remove_ads"));
            var purchaseProvider = new FakePurchaseProvider();
            var purchases = new PurchaseService(catalog, purchaseProvider);
            purchases.Initialize(registry);

            var adConfig = TestDefinitions.AdConfig(
                new[]
                {
                    TestDefinitions.AdPlacement("MainBanner", AdType.Banner),
                    TestDefinitions.AdPlacement("LevelInterstitial", AdType.Interstitial),
                    TestDefinitions.AdPlacement("RewardedRevive", AdType.Rewarded)
                },
                new[] { TestDefinitions.SuppressionRule("remove_ads", AdType.Banner, AdType.Interstitial) });
            var adProvider = new FakeAdProvider();
            var ads = new AdsService(adConfig, adProvider);
            ads.Initialize(registry);

            adProvider.RaiseLoaded(new AdPlacementId("MainBanner"));
            adProvider.RaiseLoaded(new AdPlacementId("LevelInterstitial"));
            adProvider.RaiseLoaded(new AdPlacementId("RewardedRevive"));

            Assert.AreEqual(AdAvailabilityReason.Available, ads.CanShow(new AdPlacementId("MainBanner")), "Before purchase, ads are not suppressed.");

            purchases.Purchase(new ProductId("remove_ads"), _ => { });

            Assert.IsTrue(entitlements.HasEntitlement(new EntitlementId("remove_ads")));
            Assert.AreEqual(AdAvailabilityReason.SuppressedByEntitlement, ads.CanShow(new AdPlacementId("MainBanner")));
            Assert.AreEqual(AdAvailabilityReason.SuppressedByEntitlement, ads.CanShow(new AdPlacementId("LevelInterstitial")));
            Assert.AreEqual(AdAvailabilityReason.Available, ads.CanShow(new AdPlacementId("RewardedRevive")),
                "Remove Ads must not implicitly suppress Rewarded - see CLAUDE.md's Phase 15 brief, section 21.");

            ads.Shutdown();
            purchases.Shutdown();
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(adConfig);
        }

        [Test]
        public void ConsumablePurchase_GrantsReward_AndSurvivesSimulatedRestartWithoutDoubleGranting()
        {
            var storage = new InMemoryPersistenceStorage();
            var coinsRewardDefinition = TestDefinitions.Reward("CoinsReward", RewardClaimPolicy.Repeatable);
            var coinsReward = new CountingReward();

            ServiceRegistry registry1 = TestRegistryFactory.Build(
                storage, out FakeTimeService _, out EventService _, out PersistenceService _, out TimerService _,
                out RewardService rewards1, out EntitlementService _);
            rewards1.RegisterReward(coinsRewardDefinition, coinsReward);

            var catalog = TestDefinitions.Catalog(
                TestDefinitions.Product("coins_100", ProductType.Consumable, rewardId: "CoinsReward"));
            var provider1 = new FakePurchaseProvider();
            provider1.NextPurchaseResults.Enqueue(new PurchaseResult(PurchaseResultKind.Success, new ProductId("coins_100"), "restart-txn", null));
            var purchases1 = new PurchaseService(catalog, provider1);
            purchases1.Initialize(registry1);

            purchases1.Purchase(new ProductId("coins_100"), _ => { });
            Assert.AreEqual(1, coinsReward.GrantCount);
            purchases1.Save();
            purchases1.Shutdown();

            // Simulated application restart: a fresh PurchaseService instance over the same storage.
            var coinsRewardDefinition2 = TestDefinitions.Reward("CoinsReward", RewardClaimPolicy.Repeatable);
            var coinsReward2 = new CountingReward();
            ServiceRegistry registry2 = TestRegistryFactory.Build(
                storage, out FakeTimeService _, out EventService _, out PersistenceService _, out TimerService _,
                out RewardService rewards2, out EntitlementService _);
            rewards2.RegisterReward(coinsRewardDefinition2, coinsReward2);

            var provider2 = new FakePurchaseProvider();
            var purchases2 = new PurchaseService(catalog, provider2);
            purchases2.Initialize(registry2);

            Assert.IsTrue(purchases2.IsTransactionProcessed("restart-txn"), "Processed transaction ids must survive a restart.");

            // The same store transaction being redelivered after restart (a real-world occurrence)
            // must not grant the reward again.
            provider2.RaisePurchaseUpdated(new PurchaseResult(PurchaseResultKind.Success, new ProductId("coins_100"), "restart-txn", null));
            Assert.AreEqual(0, coinsReward2.GrantCount, "A transaction already processed before restart must not grant again after restart.");

            purchases2.Shutdown();
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(coinsRewardDefinition);
            Object.DestroyImmediate(coinsRewardDefinition2);
        }

        [Test]
        public void AdPlacementRewardBridge_ClaimsMappedRewardOnRewardEarned()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(
                out FakeTimeService _, out EventService _, out PersistenceService _, out TimerService _,
                out RewardService rewards, out EntitlementService _);

            var rewardDefinition = TestDefinitions.Reward("DoubleCoins", RewardClaimPolicy.Repeatable);
            var reward = new CountingReward();
            rewards.RegisterReward(rewardDefinition, reward);

            var mapping = new Dictionary<AdPlacementId, RewardId>
            {
                { new AdPlacementId("RewardedDoubleCoins"), new RewardId("DoubleCoins") }
            };
            var bridge = new AdPlacementRewardBridge(registry, mapping);

            var adConfig = TestDefinitions.AdConfig(new[] { TestDefinitions.AdPlacement("RewardedDoubleCoins", AdType.Rewarded) });
            var adProvider = new FakeAdProvider();
            var ads = new AdsService(adConfig, adProvider);
            ads.Initialize(registry);

            adProvider.RaiseLoaded(new AdPlacementId("RewardedDoubleCoins"));
            ads.ShowRewarded(new AdPlacementId("RewardedDoubleCoins"), _ => { });
            adProvider.RaiseShown(new AdPlacementId("RewardedDoubleCoins"));
            adProvider.RaiseRewardEarned(new AdPlacementId("RewardedDoubleCoins"));
            adProvider.RaiseClosed(new AdPlacementId("RewardedDoubleCoins"));

            Assert.AreEqual(1, reward.GrantCount);

            bridge.Dispose();
            ads.Shutdown();
            Object.DestroyImmediate(adConfig);
            Object.DestroyImmediate(rewardDefinition);
        }
    }
}
