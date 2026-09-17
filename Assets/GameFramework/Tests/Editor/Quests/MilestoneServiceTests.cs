using GameFramework.Progression.Economy;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Milestones;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class MilestoneServiceTests
    {
        private StatisticDefinition _racesCompleted;
        private CurrencyDefinition _coins;
        private MilestoneDefinition _raceMaster;

        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private StatisticsService _statistics;
        private EconomyService _economy;
        private RewardService _rewards;
        private MilestoneService _milestones;

        private static MilestoneId RaceMasterId => new MilestoneId("RaceMaster");
        private static StatisticId RacesId => new StatisticId("RacesCompleted");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _raceMaster = TestDefinitions.Milestone("RaceMaster", "RacesCompleted", 100, rewardId: "RaceMasterReward");

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted });
            _statistics.Initialize(_registry);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);

            _rewards = new RewardService();
            _registry.Register<IRewardService>(_rewards);
            _rewards.Initialize(_registry);
            _registry.MarkInitialized(typeof(IRewardService));
            _rewards.RegisterReward(TestDefinitions.Reward("RaceMasterReward", RewardClaimPolicy.Once),
                new CurrencyReward(_economy, new CurrencyId("Coins"), 1000));

            _registry.Register<IStatisticsService>(_statistics);
            _registry.MarkInitialized(typeof(IStatisticsService));

            _milestones = new MilestoneService();
            _milestones.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_raceMaster);
        }

        [Test]
        public void ThresholdNotReached_IsNotReached()
        {
            _milestones.RegisterMilestone(_raceMaster);

            _statistics.Increment(RacesId, 50);

            Assert.IsFalse(_milestones.IsReached(RaceMasterId));
            MilestoneProgress progress = _milestones.GetProgress(RaceMasterId);
            Assert.AreEqual(50, progress.CurrentValue);
            Assert.AreEqual(100, progress.RequiredValue);
        }

        [Test]
        public void ThresholdReached_MarksReached_AndPublishesEvent()
        {
            _milestones.RegisterMilestone(_raceMaster);

            MilestoneReachedEvent? reached = null;
            _events.Subscribe<MilestoneReachedEvent>(e => reached = e);

            _statistics.Increment(RacesId, 100);

            Assert.IsTrue(_milestones.IsReached(RaceMasterId));
            Assert.IsTrue(reached.HasValue);
        }

        [Test]
        public void RepeatedStatisticChanges_OnlyReachOnce()
        {
            _milestones.RegisterMilestone(_raceMaster);

            int reachedCount = 0;
            _events.Subscribe<MilestoneReachedEvent>(_ => reachedCount++);

            _statistics.Increment(RacesId, 100);
            _statistics.Increment(RacesId, 10);
            _statistics.Increment(RacesId, 10);

            Assert.AreEqual(1, reachedCount);
        }

        [Test]
        public void TryClaimReward_IsIdempotent()
        {
            _milestones.RegisterMilestone(_raceMaster);
            _statistics.Increment(RacesId, 100);

            MilestoneClaimResult first = _milestones.TryClaimReward(RaceMasterId);
            MilestoneClaimResult second = _milestones.TryClaimReward(RaceMasterId);

            Assert.AreEqual(MilestoneClaimResult.Success, first);
            Assert.AreEqual(MilestoneClaimResult.AlreadyClaimed, second);
            Assert.AreEqual(1000, _economy.GetBalance(new CurrencyId("Coins")));
        }

        [Test]
        public void SaveThenLoad_PreservesReachedState()
        {
            _milestones.RegisterMilestone(_raceMaster);
            _statistics.Increment(RacesId, 100);
            _milestones.Save();

            var reloaded = new MilestoneService();
            reloaded.Initialize(_registry);
            reloaded.RegisterMilestone(_raceMaster);

            Assert.IsTrue(reloaded.IsReached(RaceMasterId));
        }
    }
}
