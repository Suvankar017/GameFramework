using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Unlocks;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Rewards.Tests
{
    public class RewardTypeTests
    {
        private CurrencyDefinition _coins;
        private ItemDefinition _potion;
        private ProgressionCurveDefinition _curve;
        private UnlockDefinition _carUnlock;

        private EconomyService _economy;
        private InventoryService _inventory;
        private ExperienceService _experience;
        private UnlockService _unlocks;

        [SetUp]
        public void SetUp()
        {
            _coins = TestDefinitions.Currency("Coins", maxBalance: 1000);
            _potion = TestDefinitions.Item("HealthPotion", maxStack: 99);
            _curve = TestDefinitions.LinearCurve(100, 0);
            _carUnlock = TestDefinitions.Unlock("Car");

            var registry = TestRegistryFactory.Build(out _, out _);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(registry);

            _inventory = new InventoryService(new[] { _potion });
            _inventory.Initialize(registry);

            _experience = new ExperienceService(_curve);
            _experience.Initialize(registry);

            _unlocks = new UnlockService();
            _unlocks.Initialize(registry);
            _unlocks.RegisterUnlock(_carUnlock, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_potion);
            Object.DestroyImmediate(_curve);
            Object.DestroyImmediate(_carUnlock);
        }

        [Test]
        public void CurrencyReward_Grant_AddsBalance()
        {
            var reward = new CurrencyReward(_economy, new CurrencyId("Coins"), 100);

            Assert.IsTrue(reward.CanGrant());
            Assert.IsTrue(reward.Grant().Success);
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")));
        }

        [Test]
        public void CurrencyReward_UnregisteredCurrency_CannotGrant()
        {
            var reward = new CurrencyReward(_economy, new CurrencyId("Gems"), 100);

            Assert.IsFalse(reward.CanGrant());
        }

        [Test]
        public void ItemReward_Grant_AddsQuantity()
        {
            var reward = new ItemReward(_inventory, new ItemId("HealthPotion"), 3);

            Assert.IsTrue(reward.CanGrant());
            Assert.IsTrue(reward.Grant().Success);
            Assert.AreEqual(3, _inventory.GetQuantity(new ItemId("HealthPotion")));
        }

        [Test]
        public void ExperienceReward_Grant_AddsExperience()
        {
            var reward = new ExperienceReward(_experience, 50);

            Assert.IsTrue(reward.Grant().Success);
            Assert.AreEqual(50, _experience.CurrentExperience);
        }

        [Test]
        public void UnlockReward_Grant_UnlocksWithoutRequirement()
        {
            var reward = new UnlockReward(_unlocks, new UnlockId("Car"));

            Assert.IsTrue(reward.Grant().Success);
            Assert.IsTrue(_unlocks.IsUnlocked(new UnlockId("Car")));
        }

        [Test]
        public void RewardBundle_AllValid_GrantsEveryChild()
        {
            var bundle = new RewardBundle(
                new CurrencyReward(_economy, new CurrencyId("Coins"), 100),
                new ItemReward(_inventory, new ItemId("HealthPotion"), 1),
                new ExperienceReward(_experience, 50));

            Assert.IsTrue(bundle.CanGrant());
            Assert.IsTrue(bundle.Grant().Success);

            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")));
            Assert.AreEqual(1, _inventory.GetQuantity(new ItemId("HealthPotion")));
            Assert.AreEqual(50, _experience.CurrentExperience);
        }

        [Test]
        public void RewardBundle_OneChildInvalid_CanGrantFalse_NothingGranted()
        {
            var bundle = new RewardBundle(
                new CurrencyReward(_economy, new CurrencyId("Coins"), 100),
                new CurrencyReward(_economy, new CurrencyId("UnregisteredCurrency"), 1)); // invalid

            Assert.IsFalse(bundle.CanGrant());

            // RewardService is the one that enforces "check CanGrant before Grant" - verify the
            // pre-check alone already reports the whole bundle as ungrantable, so a caller that
            // respects CanGrant() never mutates state for an invalid bundle.
            Assert.AreEqual(0, _economy.GetBalance(new CurrencyId("Coins")));
        }
    }
}
