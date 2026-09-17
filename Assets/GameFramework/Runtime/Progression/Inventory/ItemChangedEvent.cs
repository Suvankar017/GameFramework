namespace GameFramework.Progression.Inventory
{
    /// <summary>Published by <see cref="InventoryService"/> through the Phase 2 Event System
    /// whenever an item's owned quantity actually changes.</summary>
    public readonly struct ItemChangedEvent
    {
        public readonly ItemId Item;
        public readonly int PreviousQuantity;
        public readonly int NewQuantity;
        public readonly int Delta;
        public readonly string Reason;

        public ItemChangedEvent(ItemId item, int previousQuantity, int newQuantity, string reason)
        {
            Item = item;
            PreviousQuantity = previousQuantity;
            NewQuantity = newQuantity;
            Delta = newQuantity - previousQuantity;
            Reason = reason;
        }
    }
}
