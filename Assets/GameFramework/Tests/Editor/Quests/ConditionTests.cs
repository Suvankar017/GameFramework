using GameFramework.Progression.Statistics;
using GameFramework.Quests.Conditions;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class ConditionTests
    {
        private StatisticDefinition _racesCompleted;
        private StatisticDefinition _coinsCollected;
        private GameFramework.Runtime.Services.ServiceRegistry _registry;
        private GameFramework.Runtime.Events.EventService _events;
        private GameFramework.Runtime.Persistence.PersistenceService _persistence;
        private StatisticsService _statistics;

        private static StatisticId RacesId => new StatisticId("RacesCompleted");
        private static StatisticId CoinsId => new StatisticId("CoinsCollected");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _coinsCollected = TestDefinitions.Statistic("CoinsCollected");

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted, _coinsCollected });
            _statistics.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_coinsCollected);
        }

        [Test]
        public void StatisticCondition_ReportsProgress_AndSatisfaction()
        {
            var condition = new StatisticCondition(_statistics, RacesId, 5);

            Assert.IsFalse(condition.IsSatisfied());
            Assert.AreEqual(0, condition.CurrentValue);
            Assert.AreEqual(5, condition.RequiredValue);

            _statistics.Increment(RacesId, 5);

            Assert.IsTrue(condition.IsSatisfied());
            Assert.AreEqual(5, condition.CurrentValue);
        }

        [Test]
        public void StatisticCondition_LessThan_UsesCorrectComparison()
        {
            var condition = new StatisticCondition(_statistics, RacesId, 3, ComparisonOperator.Less);

            Assert.IsTrue(condition.IsSatisfied());

            _statistics.Increment(RacesId, 3);

            Assert.IsFalse(condition.IsSatisfied());
        }

        [Test]
        public void AllCondition_RequiresEveryChild()
        {
            var races = new StatisticCondition(_statistics, RacesId, 5);
            var coins = new StatisticCondition(_statistics, CoinsId, 100);
            var all = new AllCondition(races, coins);

            Assert.IsFalse(all.IsSatisfied());

            _statistics.Increment(RacesId, 5);
            Assert.IsFalse(all.IsSatisfied());

            _statistics.Increment(CoinsId, 100);
            Assert.IsTrue(all.IsSatisfied());
        }

        [Test]
        public void AnyCondition_RequiresOnlyOneChild()
        {
            var races = new StatisticCondition(_statistics, RacesId, 5);
            var coins = new StatisticCondition(_statistics, CoinsId, 100);
            var any = new AnyCondition(races, coins);

            Assert.IsFalse(any.IsSatisfied());

            _statistics.Increment(RacesId, 5);

            Assert.IsTrue(any.IsSatisfied());
        }

        [Test]
        public void AnyCondition_WithNoChildren_IsUnsatisfied()
        {
            var any = new AnyCondition();

            Assert.IsFalse(any.IsSatisfied());
        }

        [Test]
        public void AllCondition_WithNoChildren_IsVacuouslySatisfied()
        {
            var all = new AllCondition();

            Assert.IsTrue(all.IsSatisfied());
        }

        [Test]
        public void NotCondition_InvertsInner()
        {
            var races = new StatisticCondition(_statistics, RacesId, 5);
            var not = new NotCondition(races);

            Assert.IsTrue(not.IsSatisfied());

            _statistics.Increment(RacesId, 5);

            Assert.IsFalse(not.IsSatisfied());
        }

        [Test]
        public void NestedComposite_EvaluatesCorrectly()
        {
            // (Races >= 5 AND Coins >= 100) OR NOT(Races >= 1)
            var races5 = new StatisticCondition(_statistics, RacesId, 5);
            var coins100 = new StatisticCondition(_statistics, CoinsId, 100);
            var races1 = new StatisticCondition(_statistics, RacesId, 1);
            var nested = new AnyCondition(new AllCondition(races5, coins100), new NotCondition(races1));

            Assert.IsTrue(nested.IsSatisfied(), "No races yet -> NOT(races>=1) is true.");

            _statistics.Increment(RacesId, 1);
            Assert.IsFalse(nested.IsSatisfied());

            _statistics.Increment(RacesId, 4);
            _statistics.Increment(CoinsId, 100);
            Assert.IsTrue(nested.IsSatisfied());
        }

        [Test]
        public void StatisticFlagCondition_ChecksBooleanStatistic()
        {
            var tutorialStat = TestDefinitions.Statistic("TutorialDone", StatisticValueType.Boolean);
            var boolStatistics = new StatisticsService(new[] { tutorialStat });
            boolStatistics.Initialize(_registry);

            var condition = new StatisticFlagCondition(boolStatistics, new StatisticId("TutorialDone"));

            Assert.IsFalse(condition.IsSatisfied());

            boolStatistics.SetBool(new StatisticId("TutorialDone"), true);

            Assert.IsTrue(condition.IsSatisfied());

            Object.DestroyImmediate(tutorialStat);
        }
    }
}
