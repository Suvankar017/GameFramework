namespace GameFramework.Progression.Inventory
{
    /// <summary>
    /// Result of <see cref="IInventoryService.TryAdd"/>/<see cref="IInventoryService.TryRemove"/>.
    /// Adding more than <see cref="ItemDefinition.MaxStack"/> allows is never a silent item loss —
    /// <see cref="AppliedAmount"/> is exactly how much was actually applied, and
    /// <see cref="Remainder"/> is exactly how much was not (e.g. requested 150 into a 99-max stack
    /// starting empty: <see cref="Success"/> is true, <see cref="AppliedAmount"/> is 99,
    /// <see cref="Remainder"/> is 51 — the caller decides what to do with the remainder).
    /// </summary>
    public readonly struct InventoryOperationResult
    {
        public readonly bool Success;
        public readonly int AppliedAmount;
        public readonly int Remainder;
        public readonly InventoryFailureReason FailureReason;

        private InventoryOperationResult(bool success, int appliedAmount, int remainder, InventoryFailureReason failureReason)
        {
            Success = success;
            AppliedAmount = appliedAmount;
            Remainder = remainder;
            FailureReason = failureReason;
        }

        public static InventoryOperationResult Ok(int appliedAmount, int remainder = 0) =>
            new InventoryOperationResult(true, appliedAmount, remainder, InventoryFailureReason.None);

        public static InventoryOperationResult Fail(InventoryFailureReason reason) =>
            new InventoryOperationResult(false, 0, 0, reason);
    }
}
