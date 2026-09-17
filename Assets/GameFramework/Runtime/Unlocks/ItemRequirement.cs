using GameFramework.Core.Validation;
using GameFramework.Progression.Inventory;

namespace GameFramework.Unlocks
{
    public sealed class ItemRequirement : IUnlockRequirement
    {
        private readonly IInventoryService _inventory;
        private readonly ItemId _item;
        private readonly int _minQuantity;

        public ItemRequirement(IInventoryService inventory, ItemId item, int minQuantity = 1)
        {
            _inventory = Guard.NotNull(inventory, nameof(inventory));
            _item = item;
            _minQuantity = minQuantity;
        }

        public bool IsSatisfied() => _inventory.Has(_item, _minQuantity);

        public string Describe() => $"Own {_minQuantity} {_item}";
    }
}
