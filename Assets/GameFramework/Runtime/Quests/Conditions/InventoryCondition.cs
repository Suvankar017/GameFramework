using GameFramework.Core.Validation;
using GameFramework.Progression.Inventory;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Checks owned item quantity (e.g. "Own 5 cars") - mirrors
    /// <see cref="GameFramework.Unlocks.ItemRequirement"/>, with progress reporting added.</summary>
    public sealed class InventoryCondition : IProgressCondition
    {
        private readonly IInventoryService _inventory;
        private readonly ItemId _item;
        private readonly int _requiredQuantity;

        public InventoryCondition(IInventoryService inventory, ItemId item, int requiredQuantity = 1)
        {
            _inventory = Guard.NotNull(inventory, nameof(inventory));
            _item = item;
            _requiredQuantity = requiredQuantity;
        }

        public int CurrentValue => _inventory.GetQuantity(_item);

        public int RequiredValue => _requiredQuantity;

        public bool IsSatisfied() => _inventory.Has(_item, _requiredQuantity);

        public string Describe() => $"Own {_requiredQuantity} {_item}";
    }
}
