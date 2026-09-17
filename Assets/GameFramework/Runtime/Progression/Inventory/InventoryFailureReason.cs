namespace GameFramework.Progression.Inventory
{
    public enum InventoryFailureReason
    {
        None,
        UnknownItem,
        InvalidQuantity,
        InsufficientQuantity,

        /// <summary>The item's <see cref="ItemDefinition.MaxStack"/> was already reached — nothing
        /// was added, not even a partial amount. See <see cref="InventoryOperationResult"/>'s
        /// remarks on how a partial add is reported instead.</summary>
        StackLimitReached
    }
}
