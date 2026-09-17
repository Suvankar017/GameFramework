using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Inventory
{
    /// <summary>
    /// Generic item ownership/quantity tracking. Quantity-based (a single aggregate count per
    /// item), not slot/grid-based — the framework defines no concrete items, only how many of an
    /// <see cref="ItemId"/> a player owns.
    /// </summary>
    public interface IInventoryService : IGameService
    {
        bool IsRegistered(ItemId item);

        int GetQuantity(ItemId item);

        bool Has(ItemId item);

        bool Has(ItemId item, int quantity);

        /// <summary>Adds up to <paramref name="quantity"/> (must be &gt; 0), capped at the item's
        /// <see cref="ItemDefinition.MaxStack"/> if one is configured — see
        /// <see cref="InventoryOperationResult"/> for how a partial add is reported.</summary>
        InventoryOperationResult TryAdd(ItemId item, int quantity, string reason = null);

        /// <summary>Removes exactly <paramref name="quantity"/> (must be &gt; 0), or fails entirely
        /// with <see cref="InventoryFailureReason.InsufficientQuantity"/> if not enough is owned —
        /// never removes a partial amount.</summary>
        InventoryOperationResult TryRemove(ItemId item, int quantity, string reason = null);

        /// <summary>Sets the owned quantity directly (e.g. save-data restoration, debug tooling),
        /// clamped to [0, MaxStack].</summary>
        void SetQuantity(ItemId item, int quantity, string reason = null);

        void Save();
        void Load();

        /// <summary>Clears every owned quantity to 0 without touching saved data on disk until
        /// <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
