using GameFramework.Progression.Inventory;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Progression.Tests.Inventory
{
    public class InventoryServiceTests
    {
        private ItemDefinition _potion;
        private ServiceRegistry _registry;
        private EventService _events;
        private InventoryService _inventory;

        private static ItemId Potion => new ItemId("HealthPotion");
        private static ItemId Unknown => new ItemId("Sword");

        [SetUp]
        public void SetUp()
        {
            _potion = TestDefinitions.Item("HealthPotion", maxStack: 99);
            _registry = TestRegistryFactory.Build(out _, out _events, out _);

            _inventory = new InventoryService(new[] { _potion });
            _inventory.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_potion != null)
            {
                Object.DestroyImmediate(_potion);
            }
        }

        [Test]
        public void InitialQuantity_IsZero()
        {
            Assert.AreEqual(0, _inventory.GetQuantity(Potion));
            Assert.IsFalse(_inventory.Has(Potion));
        }

        [Test]
        public void TryAdd_ValidQuantity_IncreasesQuantity()
        {
            InventoryOperationResult result = _inventory.TryAdd(Potion, 5);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, result.AppliedAmount);
            Assert.AreEqual(0, result.Remainder);
            Assert.AreEqual(5, _inventory.GetQuantity(Potion));
        }

        [Test]
        public void TryAdd_PublishesItemChangedEvent()
        {
            ItemChangedEvent? received = null;
            _events.Subscribe<ItemChangedEvent>(e => received = e);

            _inventory.TryAdd(Potion, 3, "Loot");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(3, received.Value.NewQuantity);
            Assert.AreEqual(3, received.Value.Delta);
            Assert.AreEqual("Loot", received.Value.Reason);
        }

        [Test]
        public void TryAdd_BeyondMaxStack_ReturnsPartialAmountAndRemainder()
        {
            InventoryOperationResult result = _inventory.TryAdd(Potion, 150);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(99, result.AppliedAmount);
            Assert.AreEqual(51, result.Remainder);
            Assert.AreEqual(99, _inventory.GetQuantity(Potion));
        }

        [Test]
        public void TryAdd_AlreadyAtMaxStack_FailsWithStackLimitReached()
        {
            _inventory.TryAdd(Potion, 99);

            InventoryOperationResult result = _inventory.TryAdd(Potion, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryFailureReason.StackLimitReached, result.FailureReason);
        }

        [Test]
        public void TryAdd_UnknownItem_Fails()
        {
            InventoryOperationResult result = _inventory.TryAdd(Unknown, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryFailureReason.UnknownItem, result.FailureReason);
        }

        [Test]
        public void TryAdd_ZeroOrNegativeQuantity_Fails()
        {
            Assert.AreEqual(InventoryFailureReason.InvalidQuantity, _inventory.TryAdd(Potion, 0).FailureReason);
            Assert.AreEqual(InventoryFailureReason.InvalidQuantity, _inventory.TryAdd(Potion, -5).FailureReason);
        }

        [Test]
        public void TryRemove_SufficientQuantity_DecreasesQuantity()
        {
            _inventory.TryAdd(Potion, 10);

            InventoryOperationResult result = _inventory.TryRemove(Potion, 4);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(4, result.AppliedAmount);
            Assert.AreEqual(6, _inventory.GetQuantity(Potion));
        }

        [Test]
        public void TryRemove_InsufficientQuantity_FailsAndLeavesQuantityUnchanged()
        {
            _inventory.TryAdd(Potion, 2);

            InventoryOperationResult result = _inventory.TryRemove(Potion, 5);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryFailureReason.InsufficientQuantity, result.FailureReason);
            Assert.AreEqual(2, _inventory.GetQuantity(Potion));
        }

        [Test]
        public void Has_WithQuantity_ChecksThreshold()
        {
            _inventory.TryAdd(Potion, 3);

            Assert.IsTrue(_inventory.Has(Potion, 3));
            Assert.IsFalse(_inventory.Has(Potion, 4));
        }

        [Test]
        public void SaveThenLoad_RestoresQuantity()
        {
            _inventory.TryAdd(Potion, 7);
            _inventory.Save();

            var reloaded = new InventoryService(new[] { _potion });
            reloaded.Initialize(_registry);

            Assert.AreEqual(7, reloaded.GetQuantity(Potion));
        }

        [Test]
        public void ResetToDefaults_ClearsQuantities()
        {
            _inventory.TryAdd(Potion, 10);

            _inventory.ResetToDefaults();

            Assert.AreEqual(0, _inventory.GetQuantity(Potion));
        }
    }
}
