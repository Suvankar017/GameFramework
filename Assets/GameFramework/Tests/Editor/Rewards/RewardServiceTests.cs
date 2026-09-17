using GameFramework.Progression.Economy;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Rewards.Tests
{
    public class RewardServiceTests
    {
        private CurrencyDefinition _coins;
        private RewardDefinition _onceReward;
        private RewardDefinition _repeatableReward;
        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private EconomyService _economy;
        private RewardService _rewards;

        private static RewardId OnceId => new RewardId("LevelCompletion");
        private static RewardId RepeatableId => new RewardId("DailyBonus");

        [SetUp]
        public void SetUp()
        {
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _onceReward = TestDefinitions.Reward("LevelCompletion", RewardClaimPolicy.Once);
            _repeatableReward = TestDefinitions.Reward("DailyBonus", RewardClaimPolicy.Repeatable);

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);

            _rewards = new RewardService();
            _rewards.Initialize(_registry);
            _rewards.RegisterReward(_onceReward, new CurrencyReward(_economy, new CurrencyId("Coins"), 100));
            _rewards.RegisterReward(_repeatableReward, new CurrencyReward(_economy, new CurrencyId("Coins"), 10));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_onceReward);
            Object.DestroyImmediate(_repeatableReward);
        }

        [Test]
        public void TryClaim_FirstTime_Succeeds()
        {
            RewardClaimResult result = _rewards.TryClaim(OnceId);

            Assert.AreEqual(RewardClaimResult.Success, result);
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")));
            Assert.IsTrue(_rewards.HasClaimed(OnceId));
        }

        [Test]
        public void TryClaim_OnceReward_SecondCall_IsIdempotent()
        {
            _rewards.TryClaim(OnceId);

            RewardClaimResult second = _rewards.TryClaim(OnceId);

            Assert.AreEqual(RewardClaimResult.AlreadyClaimed, second);
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")), "Claiming twice must not grant twice.");
        }

        [Test]
        public void TryClaim_ManyRepeatedCallsOnOnceReward_GrantsExactlyOnce()
        {
            for (int i = 0; i < 5; i++)
            {
                _rewards.TryClaim(OnceId);
            }

            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")));
        }

        [Test]
        public void TryClaim_RepeatableReward_GrantsEveryCall()
        {
            _rewards.TryClaim(RepeatableId);
            _rewards.TryClaim(RepeatableId);
            _rewards.TryClaim(RepeatableId);

            Assert.AreEqual(30, _economy.GetBalance(new CurrencyId("Coins")));
            Assert.IsFalse(_rewards.HasClaimed(RepeatableId), "Repeatable rewards never track a claimed flag.");
        }

        [Test]
        public void TryClaim_UnregisteredId_ReturnsInvalidId()
        {
            Assert.AreEqual(RewardClaimResult.InvalidId, _rewards.TryClaim(new RewardId("Unknown")));
        }

        [Test]
        public void TryClaim_PublishesGrantedAndClaimedEventsForOnceReward()
        {
            RewardGrantedEvent? granted = null;
            RewardClaimedEvent? claimed = null;
            _events.Subscribe<RewardGrantedEvent>(e => granted = e);
            _events.Subscribe<RewardClaimedEvent>(e => claimed = e);

            _rewards.TryClaim(OnceId, "Test");

            Assert.IsTrue(granted.HasValue);
            Assert.IsTrue(claimed.HasValue);
            Assert.AreEqual(OnceId, granted.Value.Reward);
            Assert.AreEqual(OnceId, claimed.Value.Reward);
        }

        [Test]
        public void TryClaim_RepeatableReward_PublishesGrantedButNeverClaimed()
        {
            RewardClaimedEvent? claimed = null;
            _events.Subscribe<RewardClaimedEvent>(e => claimed = e);

            _rewards.TryClaim(RepeatableId);

            Assert.IsFalse(claimed.HasValue);
        }

        [Test]
        public void SaveThenLoad_PreservesClaimIdempotencyAcrossReload()
        {
            _rewards.TryClaim(OnceId);
            _rewards.Save();

            var reloaded = new RewardService();
            reloaded.Initialize(_registry);
            reloaded.RegisterReward(_onceReward, new CurrencyReward(_economy, new CurrencyId("Coins"), 100));

            RewardClaimResult result = reloaded.TryClaim(OnceId);

            Assert.AreEqual(RewardClaimResult.AlreadyClaimed, result);
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")), "Reload + reclaim attempt must not grant a second time.");
        }

        [Test]
        public void ResetToDefaults_AllowsOnceRewardToBeClaimedAgain()
        {
            _rewards.TryClaim(OnceId);

            _rewards.ResetToDefaults();
            RewardClaimResult result = _rewards.TryClaim(OnceId);

            Assert.AreEqual(RewardClaimResult.Success, result);
            Assert.AreEqual(200, _economy.GetBalance(new CurrencyId("Coins")));
        }
    }
}
