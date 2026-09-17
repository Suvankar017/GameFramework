using System.Collections.Generic;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Quests;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Unlocks;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    /// <summary>Cross-system flows spanning Statistics/Conditions/Objectives/Quests/Achievements/
    /// Rewards/Unlocks/Progression together - see the framework documentation's Testing section for
    /// why these live alongside, not instead of, each system's own focused unit tests.</summary>
    public class IntegrationTests
    {
        private StatisticDefinition _racesCompleted;
        private CurrencyDefinition _coins;
        private ItemDefinition _potion;
        private ProgressionCurveDefinition _curve;
        private UnlockDefinition _veteranBadge;
        private QuestDefinition _firstRace;
        private ObjectiveDefinition _completeRaceObjective;
        private AchievementDefinition _veteranAchievement;

        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private StatisticsService _statistics;
        private EconomyService _economy;
        private InventoryService _inventory;
        private ExperienceService _experience;
        private UnlockService _unlocks;
        private RewardService _rewards;
        private QuestService _quests;
        private AchievementService _achievements;

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _potion = TestDefinitions.Item("HealthPotion", maxStack: 99);
            _curve = TestDefinitions.LinearCurve(100, 0); // flat 100 XP per level
            _veteranBadge = TestDefinitions.Unlock("VeteranBadge");
            _firstRace = TestDefinitions.Quest("FirstRace", rewardId: "FirstRaceReward");
            _completeRaceObjective = TestDefinitions.Objective("CompleteRace");
            _veteranAchievement = TestDefinitions.Achievement("VeteranDriver");

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted });
            _statistics.Initialize(_registry);

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
            _registry.Register<IRewardService>(_rewards);
            _rewards.Initialize(_registry);
            _registry.MarkInitialized(typeof(IRewardService));
            _rewards.RegisterReward(TestDefinitions.Reward("FirstRaceReward", RewardClaimPolicy.Once),
                new CurrencyReward(_economy, new CurrencyId("Coins"), 100));

            _quests = new QuestService();
            _quests.Initialize(_registry);

            _achievements = new AchievementService();
            _achievements.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_potion);
            Object.DestroyImmediate(_curve);
            Object.DestroyImmediate(_veteranBadge);
            Object.DestroyImmediate(_firstRace);
            Object.DestroyImmediate(_completeRaceObjective);
            Object.DestroyImmediate(_veteranAchievement);
        }

        [Test]
        public void Flow_StatisticIncrement_ObjectiveProgress_ObjectiveCompleted_QuestCompleted()
        {
            _quests.RegisterQuest(_firstRace, null, new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRaceObjective, new StatisticCondition(_statistics, new StatisticId("RacesCompleted"), 1))
            });
            _quests.Start(new QuestId("FirstRace"));

            QuestObjectiveCompletedEvent? objectiveCompleted = null;
            QuestCompletedEvent? questCompleted = null;
            _events.Subscribe<QuestObjectiveCompletedEvent>(e => objectiveCompleted = e);
            _events.Subscribe<QuestCompletedEvent>(e => questCompleted = e);

            _statistics.Increment(new StatisticId("RacesCompleted"), 1);

            Assert.IsTrue(objectiveCompleted.HasValue);
            Assert.IsTrue(questCompleted.HasValue);
            Assert.AreEqual(QuestStatus.Completed, _quests.GetStatus(new QuestId("FirstRace")));
        }

        [Test]
        public void Flow_StatisticIncrement_AchievementCompleted_RewardClaimed_CurrencyAdded()
        {
            _rewards.RegisterReward(TestDefinitions.Reward("VeteranReward", RewardClaimPolicy.Once),
                new CurrencyReward(_economy, new CurrencyId("Coins"), 250));
            var veteranWithReward = TestDefinitions.Achievement("VeteranDriverWithReward", rewardId: "VeteranReward", autoClaimReward: true);

            _achievements.RegisterAchievement(veteranWithReward, new StatisticCondition(_statistics, new StatisticId("RacesCompleted"), 5));

            _statistics.Increment(new StatisticId("RacesCompleted"), 5);

            Assert.IsTrue(_achievements.IsCompleted(new AchievementId("VeteranDriverWithReward")));
            Assert.IsTrue(_achievements.IsClaimed(new AchievementId("VeteranDriverWithReward")));
            Assert.AreEqual(250, _economy.GetBalance(new CurrencyId("Coins")));

            Object.DestroyImmediate(veteranWithReward);
        }

        [Test]
        public void Flow_ExperienceReward_LevelUp_SatisfiesUnlockRequirement_AndAchievementCondition()
        {
            _achievements.RegisterAchievement(_veteranAchievement, new LevelCondition(_experience, 3));

            Assert.IsFalse(_unlocks.CanUnlock(new UnlockId("VeteranBadge")));
            Assert.IsFalse(_achievements.IsCompleted(new AchievementId("VeteranDriver")));

            _experience.AddExperience(250, "Test"); // 1 -> 2 -> 3 on a flat 100/level curve

            Assert.AreEqual(3, _experience.CurrentLevel);
            Assert.IsTrue(_unlocks.CanUnlock(new UnlockId("VeteranBadge")));
            Assert.IsTrue(_achievements.IsCompleted(new AchievementId("VeteranDriver")));
        }

        [Test]
        public void Flow_QuestCompleted_Save_SimulatedRestart_Load_RemainsCompleted_RewardCannotDuplicate()
        {
            _quests.RegisterQuest(_firstRace, null, new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRaceObjective, new StatisticCondition(_statistics, new StatisticId("RacesCompleted"), 1))
            });
            _quests.Start(new QuestId("FirstRace"));
            _statistics.Increment(new StatisticId("RacesCompleted"), 1);
            _quests.TryClaimReward(new QuestId("FirstRace"));

            _statistics.Save();
            _quests.Save();
            _rewards.Save();

            // Simulate a fresh session: new Statistics/Quest service instances over the same
            // persistence backend and the same (already-registered) IRewardService - a
            // ServiceRegistry entry can only be registered once, and reward-claim reload durability
            // in isolation is already covered by GameFramework.Rewards.Tests.IntegrationTests.
            var statistics2 = new StatisticsService(new[] { _racesCompleted });
            statistics2.Initialize(_registry);
            var quests2 = new QuestService();
            quests2.Initialize(_registry);
            quests2.RegisterQuest(_firstRace, null, new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRaceObjective, new StatisticCondition(statistics2, new StatisticId("RacesCompleted"), 1))
            });

            Assert.AreEqual(1, statistics2.Get(new StatisticId("RacesCompleted")));
            Assert.AreEqual(QuestStatus.Claimed, quests2.GetStatus(new QuestId("FirstRace")));

            int coinsBeforeReplay = _economy.GetBalance(new CurrencyId("Coins"));
            QuestClaimResult replay = quests2.TryClaimReward(new QuestId("FirstRace"));

            Assert.AreEqual(QuestClaimResult.AlreadyClaimed, replay);
            Assert.AreEqual(coinsBeforeReplay, _economy.GetBalance(new CurrencyId("Coins")), "Replaying the claim must not grant a second time.");
        }
    }
}
