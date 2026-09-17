using GameFramework.Progression.Statistics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    public class StatisticsServiceTests
    {
        private StatisticDefinition _racesCompleted;
        private StatisticDefinition _totalRaces;
        private StatisticDefinition _tutorialCompleted;
        private StatisticDefinition _sessionCounter;
        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private StatisticsService _statistics;

        private static StatisticId RacesId => new StatisticId("RacesCompleted");
        private static StatisticId TotalId => new StatisticId("TotalRaces");
        private static StatisticId TutorialId => new StatisticId("TutorialCompleted");
        private static StatisticId SessionId => new StatisticId("SessionCounter");

        [SetUp]
        public void SetUp()
        {
            _racesCompleted = TestDefinitions.Statistic("RacesCompleted", maxValue: 1000);
            _totalRaces = TestDefinitions.Statistic("TotalRaces", isMonotonic: true);
            _tutorialCompleted = TestDefinitions.Statistic("TutorialCompleted", StatisticValueType.Boolean);
            _sessionCounter = TestDefinitions.Statistic("SessionCounter", persistent: false);

            _registry = TestRegistryFactory.Build(out _events, out _persistence);

            _statistics = new StatisticsService(new[] { _racesCompleted, _totalRaces, _tutorialCompleted, _sessionCounter });
            _statistics.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_racesCompleted);
            Object.DestroyImmediate(_totalRaces);
            Object.DestroyImmediate(_tutorialCompleted);
            Object.DestroyImmediate(_sessionCounter);
        }

        [Test]
        public void Get_UnsetStatistic_DefaultsToZero()
        {
            Assert.AreEqual(0, _statistics.Get(RacesId));
        }

        [Test]
        public void Increment_IncreasesValue_AndPublishesEvent()
        {
            StatisticChangedEvent? changed = null;
            _events.Subscribe<StatisticChangedEvent>(e => changed = e);

            _statistics.Increment(RacesId, 3);

            Assert.AreEqual(3, _statistics.Get(RacesId));
            Assert.IsTrue(changed.HasValue);
            Assert.AreEqual(0, changed.Value.PreviousValue);
            Assert.AreEqual(3, changed.Value.NewValue);
            Assert.AreEqual(3, changed.Value.Delta);
        }

        [Test]
        public void Increment_ClampsToMaxValue()
        {
            _statistics.Increment(RacesId, 5000);

            Assert.AreEqual(1000, _statistics.Get(RacesId));
        }

        [Test]
        public void Decrement_DecreasesValue()
        {
            _statistics.Increment(RacesId, 10);
            _statistics.Decrement(RacesId, 4);

            Assert.AreEqual(6, _statistics.Get(RacesId));
        }

        [Test]
        public void Decrement_OnMonotonicStatistic_IsRejected()
        {
            _statistics.Increment(TotalId, 10);
            _statistics.Decrement(TotalId, 5);

            Assert.AreEqual(10, _statistics.Get(TotalId));
        }

        [Test]
        public void Set_OnMonotonicStatistic_RejectsLowerValue()
        {
            _statistics.Set(TotalId, 10);
            _statistics.Set(TotalId, 3);

            Assert.AreEqual(10, _statistics.Get(TotalId));
        }

        [Test]
        public void Set_OnMonotonicStatistic_AllowsHigherValue()
        {
            _statistics.Set(TotalId, 10);
            _statistics.Set(TotalId, 20);

            Assert.AreEqual(20, _statistics.Get(TotalId));
        }

        [Test]
        public void TryGet_UnregisteredId_ReturnsFalse()
        {
            bool found = _statistics.TryGet(new StatisticId("Unknown"), out int value);

            Assert.IsFalse(found);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void Increment_NonPositiveAmount_IsRejected()
        {
            _statistics.Increment(RacesId, 5);
            _statistics.Increment(RacesId, 0);
            _statistics.Increment(RacesId, -3);

            Assert.AreEqual(5, _statistics.Get(RacesId));
        }

        [Test]
        public void SetBool_ChangesValue_AndPublishesEvent()
        {
            StatisticChangedEvent? changed = null;
            _events.Subscribe<StatisticChangedEvent>(e => changed = e);

            _statistics.SetBool(TutorialId, true);

            Assert.IsTrue(_statistics.GetBool(TutorialId));
            Assert.IsTrue(changed.HasValue);
            Assert.AreEqual(1, changed.Value.NewValue);
        }

        [Test]
        public void SaveThenLoad_PreservesPersistentValues()
        {
            _statistics.Increment(RacesId, 7);
            _statistics.SetBool(TutorialId, true);
            _statistics.Save();

            var reloaded = new StatisticsService(new[] { _racesCompleted, _totalRaces, _tutorialCompleted, _sessionCounter });
            reloaded.Initialize(_registry);

            Assert.AreEqual(7, reloaded.Get(RacesId));
            Assert.IsTrue(reloaded.GetBool(TutorialId));
        }

        [Test]
        public void SaveThenLoad_DoesNotPersistSessionOnlyStatistic()
        {
            _statistics.Increment(SessionId, 42);
            _statistics.Save();

            var reloaded = new StatisticsService(new[] { _racesCompleted, _totalRaces, _tutorialCompleted, _sessionCounter });
            reloaded.Initialize(_registry);

            Assert.AreEqual(0, reloaded.Get(SessionId));
        }

        [Test]
        public void ResetToDefaults_ClearsAllValues()
        {
            _statistics.Increment(RacesId, 5);
            _statistics.Increment(TotalId, 5);

            _statistics.ResetToDefaults();

            Assert.AreEqual(0, _statistics.Get(RacesId));
            Assert.AreEqual(0, _statistics.Get(TotalId));
        }
    }
}
