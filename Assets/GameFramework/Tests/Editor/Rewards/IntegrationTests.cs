using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Unlocks;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Rewards.Tests
{
    /// <summary>Cross-system flows spanning Economy/Inventory/Experience/Unlocks/Rewards together
    /// - see the framework documentation's Testing section for why these live alongside, not
    /// instead of, each system's own focused unit tests.</summary>
    public class IntegrationTests
    {
        private CurrencyDefinition _coins;
        private ItemDefinition _potion;
        private ProgressionCurveDefinition _curve;
        private UnlockDefinition _veteranBadge;
        private RewardDefinition _questReward;

        private ServiceRegistry _registry;
        private PersistenceService _persistence;
        private EconomyService _economy;
        private InventoryService _inventory;
        private ExperienceService _experience;
        private UnlockService _unlocks;
        private RewardService _rewards;

        [SetUp]
        public void SetUp()
        {
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _potion = TestDefinitions.Item("HealthPotion", maxStack: 99);
            _curve = TestDefinitions.LinearCurve(100, 0); // flat 100 XP per level
            _veteranBadge = TestDefinitions.Unlock("VeteranBadge");
            _questReward = TestDefinitions.Reward("FirstQuest", RewardClaimPolicy.Once);

            _registry = TestRegistryFactory.Build(out _, out _persistence);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);

            _inventory = new InventoryService(new[] { _potion });
            _inventory.Initialize(_registry);

            _experience = new ExperienceService(_curve);
            _experience.Initialize(_registry);

            _unlocks = new UnlockService();
            _unlocks.Initialize(_registry);
            _unlocks.RegisterUnlock(_veteranBadge, new LevelRequirement(_experience, minLevel: 3));

            _rewards = new RewardService();
            _rewards.Initialize(_registry);
            _rewards.RegisterReward(_questReward, new RewardBundle(
                new CurrencyReward(_economy, new CurrencyId("Coins"), 100),
                new ItemReward(_inventory, new ItemId("HealthPotion"), 1),
                new ExperienceReward(_experience, 250)));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_potion);
            Object.DestroyImmediate(_curve);
            Object.DestroyImmediate(_veteranBadge);
            Object.DestroyImmediate(_questReward);
        }

        [Test]
        public void ExperienceReward_LevelUp_SatisfiesUnlockRequirement()
        {
            Assert.IsFalse(_unlocks.CanUnlock(new UnlockId("VeteranBadge")));

            // 250 XP on a flat 100/level curve: 1 -> 2 -> 3 with 50 remaining.
            _rewards.TryClaim(new RewardId("FirstQuest"));

            Assert.AreEqual(3, _experience.CurrentLevel);
            Assert.IsTrue(_unlocks.CanUnlock(new UnlockId("VeteranBadge")));

            UnlockResult unlockResult = _unlocks.TryUnlock(new UnlockId("VeteranBadge"));
            Assert.AreEqual(UnlockResult.Success, unlockResult);
        }

        [Test]
        public void RewardBundle_ClaimThenSaveThenReload_StateIsIdenticalAndNotReGranted()
        {
            _rewards.TryClaim(new RewardId("FirstQuest"));

            int coinsBefore = _economy.GetBalance(new CurrencyId("Coins"));
            int potionsBefore = _inventory.GetQuantity(new ItemId("HealthPotion"));
            int levelBefore = _experience.CurrentLevel;
            int xpBefore = _experience.CurrentExperience;

            _economy.Save();
            _inventory.Save();
            _experience.Save();
            _rewards.Save();

            // Simulate a fresh session: new instances over the same persistence backend.
            var economy2 = new EconomyService(new[] { _coins });
            economy2.Initialize(_registry);
            var inventory2 = new InventoryService(new[] { _potion });
            inventory2.Initialize(_registry);
            var experience2 = new ExperienceService(_curve);
            experience2.Initialize(_registry);
            var rewards2 = new RewardService();
            rewards2.Initialize(_registry);
            rewards2.RegisterReward(_questReward, new RewardBundle(
                new CurrencyReward(economy2, new CurrencyId("Coins"), 100),
                new ItemReward(inventory2, new ItemId("HealthPotion"), 1),
                new ExperienceReward(experience2, 250)));

            Assert.AreEqual(coinsBefore, economy2.GetBalance(new CurrencyId("Coins")));
            Assert.AreEqual(potionsBefore, inventory2.GetQuantity(new ItemId("HealthPotion")));
            Assert.AreEqual(levelBefore, experience2.CurrentLevel);
            Assert.AreEqual(xpBefore, experience2.CurrentExperience);

            RewardClaimResult replayResult = rewards2.TryClaim(new RewardId("FirstQuest"));
            Assert.AreEqual(RewardClaimResult.AlreadyClaimed, replayResult);
            Assert.AreEqual(coinsBefore, economy2.GetBalance(new CurrencyId("Coins")), "Replaying the claim must not grant a second time.");
        }

        [Test]
        public void InvalidBundleMember_RejectsWholeClaim_NoPartialStateChange()
        {
            RewardDefinition brokenReward = TestDefinitions.Reward("Broken", RewardClaimPolicy.Once);
            _rewards.RegisterReward(brokenReward, new RewardBundle(
                new CurrencyReward(_economy, new CurrencyId("Coins"), 50),
                new CurrencyReward(_economy, new CurrencyId("DoesNotExist"), 1))); // invalid currency

            RewardClaimResult result = _rewards.TryClaim(new RewardId("Broken"));

            Assert.AreEqual(RewardClaimResult.GrantFailed, result);
            Assert.AreEqual(0, _economy.GetBalance(new CurrencyId("Coins")), "No part of an invalid bundle should be granted.");
            Assert.IsFalse(_rewards.HasClaimed(new RewardId("Broken")));

            Object.DestroyImmediate(brokenReward);
        }
    }
}
