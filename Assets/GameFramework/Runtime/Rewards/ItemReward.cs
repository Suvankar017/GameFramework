using GameFramework.Core.Validation;
using GameFramework.Progression.Inventory;

namespace GameFramework.Rewards
{
    /// <summary>Grants an item. A stack-limit-clamped partial add (see
    /// <see cref="InventoryOperationResult.Remainder"/>) still counts as success here - the reward
    /// gave everything the inventory could hold, which is not the same failure as an unregistered
    /// item or invalid quantity.</summary>
    public sealed class ItemReward : IReward
    {
        private readonly IInventoryService _inventory;
        private readonly ItemId _item;
        private readonly int _quantity;

        public ItemReward(IInventoryService inventory, ItemId item, int quantity)
        {
            _inventory = Guard.NotNull(inventory, nameof(inventory));
            _item = item;
            _quantity = quantity;
        }

        public bool CanGrant() => _inventory.IsRegistered(_item) && _quantity > 0;

        public RewardGrantResult Grant()
        {
            InventoryOperationResult result = _inventory.TryAdd(_item, _quantity, "Reward");
            return result.Success
                ? RewardGrantResult.Ok()
                : RewardGrantResult.Fail($"ItemReward('{_item}', {_quantity}) -> {result.FailureReason}");
        }
    }
}
