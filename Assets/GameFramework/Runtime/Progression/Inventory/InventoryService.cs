using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Inventory
{
    /// <summary>Default <see cref="IInventoryService"/> — see <c>EconomyService</c>'s remarks for
    /// the shared constructor-injected-definitions / explicit-Save-Load-with-dirty-flag pattern
    /// this mirrors.</summary>
    public sealed class InventoryService : IInventoryService
    {
        private const string LogCategory = "Inventory";
        private const string SaveKey = "GameFramework.Progression.Inventory";
        private const int SaveVersion = 1;

        private readonly Dictionary<ItemId, ItemDefinition> _definitions = new Dictionary<ItemId, ItemDefinition>();
        private readonly Dictionary<ItemId, int> _quantities = new Dictionary<ItemId, int>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private bool _isDirty;

        public InventoryService(IEnumerable<ItemDefinition> definitions)
        {
            Guard.NotNull(definitions, nameof(definitions));

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null || !definition.Id.IsValid)
                {
                    continue;
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException($"Duplicate item id '{definition.Id}'.");
                }

                _definitions.Add(definition.Id, definition);
                _quantities.Add(definition.Id, 0);
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            Load();
        }

        public void Shutdown()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public bool IsRegistered(ItemId item) => _definitions.ContainsKey(item);

        public int GetQuantity(ItemId item) => _quantities.TryGetValue(item, out int quantity) ? quantity : 0;

        public bool Has(ItemId item) => GetQuantity(item) > 0;

        public bool Has(ItemId item, int quantity) => quantity >= 0 && GetQuantity(item) >= quantity;

        public InventoryOperationResult TryAdd(ItemId item, int quantity, string reason = null)
        {
            if (!_definitions.TryGetValue(item, out ItemDefinition definition))
            {
                LogRejected("TryAdd", item, "unregistered item");
                return InventoryOperationResult.Fail(InventoryFailureReason.UnknownItem);
            }

            if (quantity <= 0)
            {
                LogRejected("TryAdd", item, $"non-positive quantity {quantity}");
                return InventoryOperationResult.Fail(InventoryFailureReason.InvalidQuantity);
            }

            int previous = _quantities[item];
            int maxStack = definition.MaxStack;
            int room = maxStack > 0 ? Math.Max(0, maxStack - previous) : quantity;

            if (room <= 0)
            {
                return InventoryOperationResult.Fail(InventoryFailureReason.StackLimitReached);
            }

            int applied = Math.Min(quantity, room);
            int remainder = quantity - applied;
            int newQuantity = previous + applied;

            ApplyQuantity(item, previous, newQuantity, reason);
            return InventoryOperationResult.Ok(applied, remainder);
        }

        public InventoryOperationResult TryRemove(ItemId item, int quantity, string reason = null)
        {
            if (!_definitions.TryGetValue(item, out _))
            {
                LogRejected("TryRemove", item, "unregistered item");
                return InventoryOperationResult.Fail(InventoryFailureReason.UnknownItem);
            }

            if (quantity <= 0)
            {
                LogRejected("TryRemove", item, $"non-positive quantity {quantity}");
                return InventoryOperationResult.Fail(InventoryFailureReason.InvalidQuantity);
            }

            int previous = _quantities[item];
            if (previous < quantity)
            {
                return InventoryOperationResult.Fail(InventoryFailureReason.InsufficientQuantity);
            }

            int newQuantity = previous - quantity;
            ApplyQuantity(item, previous, newQuantity, reason);
            return InventoryOperationResult.Ok(quantity);
        }

        public void SetQuantity(ItemId item, int quantity, string reason = null)
        {
            if (!_definitions.TryGetValue(item, out ItemDefinition definition))
            {
                LogRejected("SetQuantity", item, "unregistered item");
                return;
            }

            int previous = _quantities[item];
            int clamped = ClampToDefinition(definition, quantity);
            ApplyQuantity(item, previous, clamped, reason);
        }

        public void Save()
        {
            var data = new InventorySaveData();
            foreach (KeyValuePair<ItemId, int> pair in _quantities)
            {
                data.ItemIds.Add(pair.Key.Value);
                data.Quantities.Add(pair.Value);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            InventorySaveData data = _persistence.Load(SaveKey, SaveVersion, new InventorySaveData());

            for (int i = 0; i < data.ItemIds.Count && i < data.Quantities.Count; i++)
            {
                var item = new ItemId(data.ItemIds[i]);
                if (!_definitions.TryGetValue(item, out ItemDefinition definition))
                {
                    continue; // item no longer exists in this build - ignore, not an error
                }

                _quantities[item] = ClampToDefinition(definition, data.Quantities[i]);
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            foreach (ItemId item in new List<ItemId>(_quantities.Keys))
            {
                int previous = _quantities[item];
                ApplyQuantity(item, previous, 0, "Reset");
            }
        }

        private void ApplyQuantity(ItemId item, int previous, int newQuantity, string reason)
        {
            _quantities[item] = newQuantity;
            if (newQuantity == previous)
            {
                return;
            }

            _isDirty = true;
            _events.Publish(new ItemChangedEvent(item, previous, newQuantity, reason));
        }

        private static int ClampToDefinition(ItemDefinition definition, int value)
        {
            if (value < 0)
            {
                value = 0;
            }

            int maxStack = definition.MaxStack;
            if (maxStack > 0 && value > maxStack)
            {
                value = maxStack;
            }

            return value;
        }

        private void LogRejected(string operation, ItemId item, string detail)
        {
            _log?.Log(LogLevel.Warning, LogCategory, $"{operation}('{item}') rejected: {detail}.");
        }
    }
}
