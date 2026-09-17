using System.Collections.Generic;
using GameFramework.Progression.Experience;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Progression.Tests.Experience
{
    public class ExperienceServiceTests
    {
        private ProgressionCurveDefinition _curve;
        private ServiceRegistry _registry;
        private EventService _events;
        private ExperienceService _experience;

        [SetUp]
        public void SetUp()
        {
            // Level 1 -> 2 costs 100, 2 -> 3 costs 100, ... (flat for predictable test math).
            _curve = TestDefinitions.LinearCurve(baseExperience: 100, incrementPerLevel: 0);
            _registry = TestRegistryFactory.Build(out _, out _events, out _);

            _experience = new ExperienceService(_curve);
            _experience.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_curve != null)
            {
                Object.DestroyImmediate(_curve);
            }
        }

        [Test]
        public void InitialState_IsLevelOneWithZeroExperience()
        {
            Assert.AreEqual(1, _experience.CurrentLevel);
            Assert.AreEqual(0, _experience.CurrentExperience);
        }

        [Test]
        public void AddExperience_BelowThreshold_OnlyIncreasesExperience()
        {
            AddExperienceResult result = _experience.AddExperience(50);

            Assert.AreEqual(AddExperienceResult.Success, result);
            Assert.AreEqual(1, _experience.CurrentLevel);
            Assert.AreEqual(50, _experience.CurrentExperience);
        }

        [Test]
        public void AddExperience_ExactlyAtThreshold_LevelsUpWithZeroRemainder()
        {
            _experience.AddExperience(100);

            Assert.AreEqual(2, _experience.CurrentLevel);
            Assert.AreEqual(0, _experience.CurrentExperience);
        }

        [Test]
        public void AddExperience_LargeGrant_CrossesMultipleLevelsDeterministically()
        {
            // Level 1, 950 XP granted at once with a flat 100-per-level curve:
            // 950 / 100 = 9 level-ups with 50 XP remaining -> Level 10, 50 XP.
            AddExperienceResult result = _experience.AddExperience(950);

            Assert.AreEqual(AddExperienceResult.Success, result);
            Assert.AreEqual(10, _experience.CurrentLevel);
            Assert.AreEqual(50, _experience.CurrentExperience);
        }

        [Test]
        public void AddExperience_MultipleLevelUps_PublishesOneLevelChangedEventPerLevelCrossed()
        {
            var levelUps = new List<LevelChangedEvent>();
            _events.Subscribe<LevelChangedEvent>(levelUps.Add);

            _experience.AddExperience(350); // 3 level-ups: 1->2, 2->3, 3->4

            Assert.AreEqual(3, levelUps.Count);
            Assert.AreEqual((1, 2), (levelUps[0].PreviousLevel, levelUps[0].NewLevel));
            Assert.AreEqual((2, 3), (levelUps[1].PreviousLevel, levelUps[1].NewLevel));
            Assert.AreEqual((3, 4), (levelUps[2].PreviousLevel, levelUps[2].NewLevel));
        }

        [Test]
        public void AddExperience_PublishesExperienceChangedEvent()
        {
            ExperienceChangedEvent? received = null;
            _events.Subscribe<ExperienceChangedEvent>(e => received = e);

            _experience.AddExperience(30, "Quest");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(30, received.Value.NewExperience);
            Assert.AreEqual("Quest", received.Value.Reason);
        }

        [Test]
        public void AddExperience_ZeroOrNegative_Rejected()
        {
            Assert.AreEqual(AddExperienceResult.InvalidAmount, _experience.AddExperience(0));
            Assert.AreEqual(AddExperienceResult.InvalidAmount, _experience.AddExperience(-10));
            Assert.AreEqual(0, _experience.CurrentExperience);
        }

        [Test]
        public void AddExperience_AtMaxLevel_IsANoOp()
        {
            ProgressionCurveDefinition cappedCurve = TestDefinitions.LinearCurve(100, 0, maxLevel: 2);
            var experience = new ExperienceService(cappedCurve);
            experience.Initialize(TestRegistryFactory.Build(out _, out _, out _));
            experience.AddExperience(100); // reaches level 2, the configured max

            AddExperienceResult result = experience.AddExperience(500);

            Assert.AreEqual(AddExperienceResult.AtMaxLevel, result);
            Assert.AreEqual(2, experience.CurrentLevel);
            Assert.IsTrue(experience.IsAtMaxLevel);

            Object.DestroyImmediate(cappedCurve);
        }

        [Test]
        public void ExperienceToNextLevel_ReflectsRemainingRequirement()
        {
            _experience.AddExperience(30);

            Assert.AreEqual(70, _experience.ExperienceToNextLevel);
        }

        [Test]
        public void TableCurve_UsesExplicitPerLevelValues()
        {
            ProgressionCurveDefinition table = TestDefinitions.TableCurve(10, 20, 30);
            var experience = new ExperienceService(table);
            experience.Initialize(TestRegistryFactory.Build(out _, out _, out _));

            experience.AddExperience(35); // 10 (->L2) + 20 (->L3) = 30, 5 remaining

            Assert.AreEqual(3, experience.CurrentLevel);
            Assert.AreEqual(5, experience.CurrentExperience);

            Object.DestroyImmediate(table);
        }

        [Test]
        public void SaveThenLoad_RestoresLevelAndExperience()
        {
            _experience.AddExperience(250);
            _experience.Save();

            var reloaded = new ExperienceService(_curve);
            reloaded.Initialize(_registry);

            Assert.AreEqual(_experience.CurrentLevel, reloaded.CurrentLevel);
            Assert.AreEqual(_experience.CurrentExperience, reloaded.CurrentExperience);
        }

        [Test]
        public void ResetToDefaults_ReturnsToLevelOneZeroExperience()
        {
            _experience.AddExperience(250);

            _experience.ResetToDefaults();

            Assert.AreEqual(1, _experience.CurrentLevel);
            Assert.AreEqual(0, _experience.CurrentExperience);
        }
    }
}
