using System.Collections.Generic;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Quests;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class QuestServiceTests
    {
        private StatisticDefinition _racesCompleted;
        private StatisticDefinition _coinsCollected;
        private CurrencyDefinition _coins;
        private QuestDefinition _firstRace;
        private QuestDefinition _repeatableQuest;
        private ObjectiveDefinition _completeRacesObjective;
        private ObjectiveDefinition _collectCoinsObjective;

        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private StatisticsService _statistics;
        private EconomyService _economy;
        private RewardService _rewards;
        private QuestService _quests;

        private static QuestId FirstRaceId => new QuestId("FirstRace");
        private static QuestId RepeatableId => new QuestId("DailyRace");
        private static StatisticId RacesId => new StatisticId("RacesCompleted");
        private static StatisticId CoinsId => new StatisticId("CoinsCollected");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _coinsCollected = TestDefinitions.Statistic("CoinsCollected");
            _coins = TestDefinitions.Currency("Coins", maxBalance: 100000);
            _completeRacesObjective = TestDefinitions.Objective("CompleteRaces", "Complete 1 race");
            _collectCoinsObjective = TestDefinitions.Objective("CollectCoins", "Collect 50 coins");

            _firstRace = TestDefinitions.Quest("FirstRace", rewardId: "FirstRaceReward");
            _repeatableQuest = TestDefinitions.Quest("DailyRace", repeatPolicy: QuestRepeatPolicy.Repeatable);

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted, _coinsCollected });
            _statistics.Initialize(_registry);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);

            _rewards = new RewardService();
            _registry.Register<IRewardService>(_rewards);
            _rewards.Initialize(_registry);
            _registry.MarkInitialized(typeof(IRewardService));
            RewardDefinition rewardDefinition = TestDefinitions.Reward("FirstRaceReward", RewardClaimPolicy.Once);
            _rewards.RegisterReward(rewardDefinition, new CurrencyReward(_economy, new CurrencyId("Coins"), 100));

            _quests = new QuestService();
            _quests.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_coinsCollected);
            Object.DestroyImmediate(_coins);
            Object.DestroyImmediate(_firstRace);
            Object.DestroyImmediate(_repeatableQuest);
            Object.DestroyImmediate(_completeRacesObjective);
            Object.DestroyImmediate(_collectCoinsObjective);
        }

        private void RegisterFirstRace(ICondition availability = null)
        {
            var objectives = new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRacesObjective, new StatisticCondition(_statistics, RacesId, 1)),
                new QuestObjectiveEntry(_collectCoinsObjective, new StatisticCondition(_statistics, CoinsId, 50))
            };

            _quests.RegisterQuest(_firstRace, availability, objectives);
        }

        [Test]
        public void UnregisteredQuest_ReportsLockedAndInvalidResults()
        {
            Assert.AreEqual(QuestStatus.Locked, _quests.GetStatus(FirstRaceId));
            Assert.AreEqual(QuestStartResult.InvalidId, _quests.Start(FirstRaceId));
        }

        [Test]
        public void RegisterQuest_WithNoAvailability_IsImmediatelyAvailable()
        {
            RegisterFirstRace();

            Assert.AreEqual(QuestStatus.Available, _quests.GetStatus(FirstRaceId));
            Assert.IsTrue(_quests.IsAvailable(FirstRaceId));
        }

        [Test]
        public void RegisterQuest_WithUnmetAvailability_IsLocked()
        {
            RegisterFirstRace(new StatisticCondition(_statistics, RacesId, 10));

            Assert.AreEqual(QuestStatus.Locked, _quests.GetStatus(FirstRaceId));
            Assert.AreEqual(QuestStartResult.NotAvailable, _quests.Start(FirstRaceId));
        }

        [Test]
        public void Start_MovesQuestToActive_AndActivatesObjectives()
        {
            RegisterFirstRace();

            QuestStartResult result = _quests.Start(FirstRaceId);

            Assert.AreEqual(QuestStartResult.Success, result);
            Assert.AreEqual(QuestStatus.Active, _quests.GetStatus(FirstRaceId));
        }

        [Test]
        public void Start_Twice_ReturnsAlreadyActive()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);

            Assert.AreEqual(QuestStartResult.AlreadyActive, _quests.Start(FirstRaceId));
        }

        [Test]
        public void AllObjectivesCompleted_CompletesQuest_AndPublishesEvent()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);

            QuestCompletedEvent? completed = null;
            _events.Subscribe<QuestCompletedEvent>(e => completed = e);

            _statistics.Increment(RacesId, 1);
            Assert.AreEqual(QuestStatus.Active, _quests.GetStatus(FirstRaceId), "Only one of two objectives satisfied.");

            _statistics.Increment(CoinsId, 50);

            Assert.AreEqual(QuestStatus.Completed, _quests.GetStatus(FirstRaceId));
            Assert.IsTrue(completed.HasValue);
        }

        [Test]
        public void UnrelatedStatisticChange_DoesNotAffectQuest()
        {
            var otherStat = TestDefinitions.Statistic("Unrelated");
            var otherStatistics = new StatisticsService(new[] { _racesCompleted, _coinsCollected, otherStat });
            otherStatistics.Initialize(_registry);

            RegisterFirstRace();
            _quests.Start(FirstRaceId);

            otherStatistics.Increment(new StatisticId("Unrelated"), 100);

            Assert.AreEqual(QuestStatus.Active, _quests.GetStatus(FirstRaceId));

            Object.DestroyImmediate(otherStat);
        }

        [Test]
        public void AnyCompletionRule_CompletesOnFirstObjective()
        {
            var anyQuest = TestDefinitions.Quest("AnyQuest", QuestCompletionRule.Any);
            var objectives = new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRacesObjective, new StatisticCondition(_statistics, RacesId, 1)),
                new QuestObjectiveEntry(_collectCoinsObjective, new StatisticCondition(_statistics, CoinsId, 50))
            };
            _quests.RegisterQuest(anyQuest, null, objectives);
            _quests.Start(new QuestId("AnyQuest"));

            _statistics.Increment(RacesId, 1);

            Assert.AreEqual(QuestStatus.Completed, _quests.GetStatus(new QuestId("AnyQuest")));

            Object.DestroyImmediate(anyQuest);
        }

        [Test]
        public void GetProgress_ReflectsFractionOfRequiredObjectives()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);

            Assert.AreEqual(0f, _quests.GetProgress(FirstRaceId));

            _statistics.Increment(RacesId, 1);

            Assert.AreEqual(0.5f, _quests.GetProgress(FirstRaceId));
        }

        [Test]
        public void TryClaimReward_BeforeCompletion_ReturnsNotCompleted()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);

            Assert.AreEqual(QuestClaimResult.NotCompleted, _quests.TryClaimReward(FirstRaceId));
        }

        [Test]
        public void TryClaimReward_AfterCompletion_GrantsRewardAndIsIdempotent()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);
            _statistics.Increment(RacesId, 1);
            _statistics.Increment(CoinsId, 50);

            QuestClaimResult first = _quests.TryClaimReward(FirstRaceId);
            QuestClaimResult second = _quests.TryClaimReward(FirstRaceId);

            Assert.AreEqual(QuestClaimResult.Success, first);
            Assert.AreEqual(QuestClaimResult.AlreadyClaimed, second);
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")), "Claiming twice must not grant twice.");
            Assert.IsTrue(_quests.IsClaimed(FirstRaceId));
            Assert.AreEqual(QuestStatus.Claimed, _quests.GetStatus(FirstRaceId));
        }

        [Test]
        public void TryReset_OnOneTimeQuest_IsRejected()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);
            _statistics.Increment(RacesId, 1);
            _statistics.Increment(CoinsId, 50);

            Assert.AreEqual(QuestResetResult.NotRepeatable, _quests.TryReset(FirstRaceId));
        }

        [Test]
        public void TryReset_OnRepeatableQuest_AllowsStartingAgain()
        {
            var objectives = new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRacesObjective, new StatisticCondition(_statistics, RacesId, 1))
            };
            _quests.RegisterQuest(_repeatableQuest, null, objectives);
            _quests.Start(RepeatableId);
            _statistics.Increment(RacesId, 1);
            Assert.AreEqual(QuestStatus.Completed, _quests.GetStatus(RepeatableId));

            QuestResetResult resetResult = _quests.TryReset(RepeatableId);

            Assert.AreEqual(QuestResetResult.Success, resetResult);
            Assert.AreEqual(QuestStatus.Available, _quests.GetStatus(RepeatableId));
            Assert.AreEqual(QuestStartResult.Success, _quests.Start(RepeatableId));
        }

        [Test]
        public void RegisterQuest_DuplicateId_Throws()
        {
            RegisterFirstRace();

            Assert.Throws<System.InvalidOperationException>(() => RegisterFirstRace());
        }

        [Test]
        public void SaveThenLoad_PreservesActiveQuestProgress()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);
            _statistics.Increment(RacesId, 1);
            _quests.Save();
            _statistics.Save();

            var reloadedStatistics = new StatisticsService(new[] { _racesCompleted, _coinsCollected });
            reloadedStatistics.Initialize(_registry);

            var reloadedQuests = new QuestService();
            reloadedQuests.Initialize(_registry);
            var reloadedObjectives = new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRacesObjective, new StatisticCondition(reloadedStatistics, RacesId, 1)),
                new QuestObjectiveEntry(_collectCoinsObjective, new StatisticCondition(reloadedStatistics, CoinsId, 50))
            };
            reloadedQuests.RegisterQuest(_firstRace, null, reloadedObjectives);

            Assert.AreEqual(QuestStatus.Active, reloadedQuests.GetStatus(FirstRaceId));
            IReadOnlyList<QuestObjectiveProgress> progress = reloadedQuests.GetObjectiveProgress(FirstRaceId);
            Assert.IsTrue(progress[0].IsCompleted, "RacesCompleted objective should already show completed after reload.");
        }

        [Test]
        public void SaveThenLoad_PreservesCompletedQuest_AndRewardCannotDuplicate()
        {
            RegisterFirstRace();
            _quests.Start(FirstRaceId);
            _statistics.Increment(RacesId, 1);
            _statistics.Increment(CoinsId, 50);
            _quests.TryClaimReward(FirstRaceId);
            _quests.Save();
            _rewards.Save();

            var reloadedQuests = new QuestService();
            reloadedQuests.Initialize(_registry);
            reloadedQuests.RegisterQuest(_firstRace, null, new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRacesObjective, new StatisticCondition(_statistics, RacesId, 1)),
                new QuestObjectiveEntry(_collectCoinsObjective, new StatisticCondition(_statistics, CoinsId, 50))
            });

            Assert.AreEqual(QuestStatus.Claimed, reloadedQuests.GetStatus(FirstRaceId));
            Assert.AreEqual(QuestClaimResult.AlreadyClaimed, reloadedQuests.TryClaimReward(FirstRaceId));
            Assert.AreEqual(100, _economy.GetBalance(new CurrencyId("Coins")));
        }
    }
}
