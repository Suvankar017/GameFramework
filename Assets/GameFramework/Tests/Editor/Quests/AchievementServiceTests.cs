using GameFramework.Progression.Economy;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Conditions;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class AchievementServiceTests
    {
        private StatisticDefinition _racesCompleted;
        private CurrencyDefinition _coins;
        private AchievementDefinition _experiencedDriver;
        private AchievementDefinition _autoClaimAchievement;

        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private StatisticsService _statistics;
        private EconomyService _economy;
        private RewardService _rewards;
        private AchievementService _achievements;

        private static AchievementId ExperiencedDriverId => new AchievementId("ExperiencedDriver");
        private static StatisticId RacesId => new StatisticId("RacesCompleted");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _experiencedDriver = TestDefinitions.Achievement("ExperiencedDriver", rewardId: "ExperiencedDriverReward");
            _autoClaimAchievement = TestDefinitions.Achievement("AutoClaim", rewardId: "AutoClaimReward", autoClaimReward: true);

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted });
            _statistics.Initialize(_registry);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);

            _rewards = new RewardService();
            _registry.Register<IRewardService>(_rewards);
            _rewards.Initialize(_registry);
            _registry.MarkInitialized(typeof(IRewardService));
            _rewards.RegisterReward(TestDefinitions.Reward("ExperiencedDriverReward", RewardClaimPolicy.Once),
                new CurrencyReward(_economy, new CurrencyId("Coins"), 500));
            _rewards.RegisterReward(TestDefinitions.Reward("AutoClaimReward", RewardClaimPolicy.Once),
                new CurrencyReward(_economy, new CurrencyId("Coins"), 10));

            _achievements = new AchievementService();
            _achievements.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_experiencedDriver);
            Object.DestroyImmediate(_autoClaimAchievement);
        }

        [Test]
        public void RegisterAchievement_StartsInProgressImmediately()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));

            Assert.IsFalse(_achievements.IsCompleted(ExperiencedDriverId));
            AchievementProgress progress = _achievements.GetProgress(ExperiencedDriverId);
            Assert.AreEqual(0, progress.CurrentValue);
            Assert.AreEqual(10, progress.RequiredValue);
        }

        [Test]
        public void StatisticReachesThreshold_CompletesAchievement_AndPublishesEvent()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));

            AchievementCompletedEvent? completed = null;
            _events.Subscribe<AchievementCompletedEvent>(e => completed = e);

            _statistics.Increment(RacesId, 10);

            Assert.IsTrue(_achievements.IsCompleted(ExperiencedDriverId));
            Assert.IsTrue(completed.HasValue);
        }

        [Test]
        public void TryClaimReward_BeforeCompletion_ReturnsNotCompleted()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));

            Assert.AreEqual(AchievementClaimResult.NotCompleted, _achievements.TryClaimReward(ExperiencedDriverId));
        }

        [Test]
        public void TryClaimReward_IsIdempotent()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));
            _statistics.Increment(RacesId, 10);

            AchievementClaimResult first = _achievements.TryClaimReward(ExperiencedDriverId);
            AchievementClaimResult second = _achievements.TryClaimReward(ExperiencedDriverId);

            Assert.AreEqual(AchievementClaimResult.Success, first);
            Assert.AreEqual(AchievementClaimResult.AlreadyClaimed, second);
            Assert.AreEqual(500, _economy.GetBalance(new CurrencyId("Coins")));
        }

        [Test]
        public void AutoClaimReward_GrantsImmediatelyOnCompletion()
        {
            var autoStat = TestDefinitions.Statistic("AutoStat");
            var autoStatistics = new StatisticsService(new[] { autoStat });
            autoStatistics.Initialize(_registry);

            _achievements.RegisterAchievement(_autoClaimAchievement, new StatisticCondition(autoStatistics, new StatisticId("AutoStat"), 1));

            autoStatistics.Increment(new StatisticId("AutoStat"), 1);

            Assert.IsTrue(_achievements.IsClaimed(new AchievementId("AutoClaim")));
            Assert.AreEqual(10, _economy.GetBalance(new CurrencyId("Coins")));

            Object.DestroyImmediate(autoStat);
        }

        [Test]
        public void SaveThenLoad_PreservesCompletion_AndPreventsDuplicateReward()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));
            _statistics.Increment(RacesId, 10);
            _achievements.TryClaimReward(ExperiencedDriverId);
            _achievements.Save();

            var reloaded = new AchievementService();
            reloaded.Initialize(_registry);
            reloaded.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));

            Assert.IsTrue(reloaded.IsCompleted(ExperiencedDriverId));
            Assert.AreEqual(AchievementClaimResult.AlreadyClaimed, reloaded.TryClaimReward(ExperiencedDriverId));
            Assert.AreEqual(500, _economy.GetBalance(new CurrencyId("Coins")));
        }

        [Test]
        public void RegisterAchievement_DuplicateId_Throws()
        {
            _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10));

            Assert.Throws<System.InvalidOperationException>(() =>
                _achievements.RegisterAchievement(_experiencedDriver, new StatisticCondition(_statistics, RacesId, 10)));
        }
    }
}
