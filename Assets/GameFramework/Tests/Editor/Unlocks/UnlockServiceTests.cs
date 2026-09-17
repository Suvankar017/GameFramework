using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Unlocks.Tests
{
    public class UnlockServiceTests
    {
        private UnlockDefinition _carUnlock;
        private ServiceRegistry _registry;
        private EventService _events;
        private UnlockService _unlocks;

        private static UnlockId CarId => new UnlockId("Car");
        private static UnlockId UnknownId => new UnlockId("Unknown");

        [SetUp]
        public void SetUp()
        {
            _carUnlock = TestDefinitions.Unlock("Car");
            _registry = TestRegistryFactory.Build(out _events, out _);

            _unlocks = new UnlockService();
            _unlocks.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_carUnlock != null)
            {
                Object.DestroyImmediate(_carUnlock);
            }
        }

        [Test]
        public void RegisterUnlock_NoRequirement_CanUnlockImmediately()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);

            Assert.IsTrue(_unlocks.CanUnlock(CarId));
        }

        [Test]
        public void RegisterUnlock_Duplicate_Throws()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);

            Assert.Throws<System.InvalidOperationException>(() => _unlocks.RegisterUnlock(_carUnlock, null));
        }

        [Test]
        public void CanUnlock_UnsatisfiedRequirement_IsFalse()
        {
            _unlocks.RegisterUnlock(_carUnlock, new FakeRequirement(false, "Reach level 10"));

            Assert.IsFalse(_unlocks.CanUnlock(CarId));
            Assert.AreEqual("Reach level 10", _unlocks.GetBlockingReason(CarId));
        }

        [Test]
        public void TryUnlock_RequirementSatisfied_Succeeds()
        {
            _unlocks.RegisterUnlock(_carUnlock, new FakeRequirement(true));

            UnlockResult result = _unlocks.TryUnlock(CarId);

            Assert.AreEqual(UnlockResult.Success, result);
            Assert.IsTrue(_unlocks.IsUnlocked(CarId));
        }

        [Test]
        public void TryUnlock_RequirementNotMet_Fails()
        {
            _unlocks.RegisterUnlock(_carUnlock, new FakeRequirement(false));

            UnlockResult result = _unlocks.TryUnlock(CarId);

            Assert.AreEqual(UnlockResult.RequirementNotMet, result);
            Assert.IsFalse(_unlocks.IsUnlocked(CarId));
        }

        [Test]
        public void TryUnlock_AlreadyUnlocked_ReturnsAlreadyUnlocked()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);
            _unlocks.TryUnlock(CarId);

            UnlockResult result = _unlocks.TryUnlock(CarId);

            Assert.AreEqual(UnlockResult.AlreadyUnlocked, result);
        }

        [Test]
        public void TryUnlock_UnregisteredId_ReturnsInvalidId()
        {
            Assert.AreEqual(UnlockResult.InvalidId, _unlocks.TryUnlock(UnknownId));
        }

        [Test]
        public void TryUnlock_PublishesUnlockChangedEvent()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);
            UnlockChangedEvent? received = null;
            _events.Subscribe<UnlockChangedEvent>(e => received = e);

            _unlocks.TryUnlock(CarId, "Debug");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(CarId, received.Value.Unlock);
            Assert.AreEqual("Debug", received.Value.Reason);
        }

        [Test]
        public void ForceUnlock_BypassesRequirement()
        {
            _unlocks.RegisterUnlock(_carUnlock, new FakeRequirement(false));

            UnlockResult result = _unlocks.ForceUnlock(CarId, "Reward");

            Assert.AreEqual(UnlockResult.Success, result);
            Assert.IsTrue(_unlocks.IsUnlocked(CarId));
        }

        [Test]
        public void PrerequisiteRequirement_TargetNotYetUnlocked_BlocksUnlock()
        {
            UnlockDefinition garage = TestDefinitions.Unlock("Garage");
            var garageRequirement = new PrerequisiteUnlockRequirement(_unlocks, new UnlockId("Garage"));
            _unlocks.RegisterUnlock(garage, null);
            _unlocks.RegisterUnlock(_carUnlock, garageRequirement);

            Assert.IsFalse(_unlocks.CanUnlock(CarId));

            _unlocks.TryUnlock(new UnlockId("Garage"));

            Assert.IsTrue(_unlocks.CanUnlock(CarId));

            Object.DestroyImmediate(garage);
        }

        [Test]
        public void ValidateNoCycles_DirectCycle_IsDetected()
        {
            UnlockDefinition a = TestDefinitions.Unlock("A");
            UnlockDefinition b = TestDefinitions.Unlock("B");
            var aRequiresB = new PrerequisiteUnlockRequirement(_unlocks, new UnlockId("B"));
            var bRequiresA = new PrerequisiteUnlockRequirement(_unlocks, new UnlockId("A"));

            _unlocks.RegisterUnlock(a, aRequiresB);
            _unlocks.RegisterUnlock(b, bRequiresA);

            IReadOnlyList<UnlockId> cycle = _unlocks.ValidateNoCycles();

            Assert.AreEqual(2, cycle.Count);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void ValidateNoCycles_NoCycle_ReturnsEmpty()
        {
            UnlockDefinition garage = TestDefinitions.Unlock("Garage");
            var garageRequirement = new PrerequisiteUnlockRequirement(_unlocks, new UnlockId("Garage"));
            _unlocks.RegisterUnlock(garage, null);
            _unlocks.RegisterUnlock(_carUnlock, garageRequirement);

            Assert.AreEqual(0, _unlocks.ValidateNoCycles().Count);

            Object.DestroyImmediate(garage);
        }

        [Test]
        public void SaveThenLoad_RestoresUnlockedState()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);
            _unlocks.TryUnlock(CarId);
            _unlocks.Save();

            var reloaded = new UnlockService();
            reloaded.Initialize(_registry);
            reloaded.RegisterUnlock(_carUnlock, null);

            Assert.IsTrue(reloaded.IsUnlocked(CarId));
        }

        [Test]
        public void ResetToDefaults_ClearsUnlockedState()
        {
            _unlocks.RegisterUnlock(_carUnlock, null);
            _unlocks.TryUnlock(CarId);

            _unlocks.ResetToDefaults();

            Assert.IsFalse(_unlocks.IsUnlocked(CarId));
        }
    }
}
