using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class ConditionObjectiveTests
    {
        private StatisticDefinition _racesCompleted;
        private GameFramework.Runtime.Services.ServiceRegistry _registry;
        private GameFramework.Runtime.Events.EventService _events;
        private GameFramework.Runtime.Persistence.PersistenceService _persistence;
        private StatisticsService _statistics;

        private static StatisticId RacesId => new StatisticId("RacesCompleted");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted");
            _registry = TestRegistryFactory.Build(out _events, out _persistence);
            _statistics = new StatisticsService(new[] { _racesCompleted });
            _statistics.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
        }

        [Test]
        public void Inactive_ReportsZeroProgress_ButStillReadsCondition()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 5));

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
            Assert.AreEqual(0, objective.CurrentValue);
            Assert.AreEqual(5, objective.RequiredValue);
        }

        [Test]
        public void Evaluate_WhileInactive_DoesNotComplete()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 1));

            _statistics.Increment(RacesId, 1);
            objective.Evaluate();

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
        }

        [Test]
        public void Evaluate_WhileActiveAndSatisfied_Completes()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 3));
            objective.Activate();

            _statistics.Increment(RacesId, 3);
            objective.Evaluate();

            Assert.AreEqual(ObjectiveState.Completed, objective.State);
        }

        [Test]
        public void Evaluate_WhileActiveAndUnsatisfied_StaysActive()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 3));
            objective.Activate();

            _statistics.Increment(RacesId, 1);
            objective.Evaluate();

            Assert.AreEqual(ObjectiveState.Active, objective.State);
            Assert.AreEqual(1, objective.CurrentValue);
        }

        [Test]
        public void ProgressNormalized_ClampsToOne()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 5));
            objective.Activate();

            _statistics.Increment(RacesId, 10); // overshoot

            Assert.AreEqual(1f, objective.ProgressNormalized);
        }

        [Test]
        public void Reset_ReturnsCompletedObjectiveToInactive()
        {
            var objective = new ConditionObjective("CompleteRaces", new StatisticCondition(_statistics, RacesId, 1));
            objective.Activate();
            _statistics.Increment(RacesId, 1);
            objective.Evaluate();
            Assert.AreEqual(ObjectiveState.Completed, objective.State);

            objective.Reset();

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
        }
    }
}
