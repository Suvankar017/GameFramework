namespace GameFramework.Monetization.Purchases
{
    /// <summary>Published by <see cref="PurchaseService"/> through the Phase 2 Event System,
    /// mirroring its own C# events - see CLAUDE.md's Phase 15 brief, sections 37/51.</summary>
    public readonly struct PurchaseCompletedEvent
    {
        public readonly PurchaseResult Result;
        public PurchaseCompletedEvent(PurchaseResult result) => Result = result;
    }

    public readonly struct PurchaseFailedEvent
    {
        public readonly PurchaseResult Result;
        public PurchaseFailedEvent(PurchaseResult result) => Result = result;
    }

    public readonly struct RestoreCompletedEvent
    {
        public readonly RestoreResult Result;
        public RestoreCompletedEvent(RestoreResult result) => Result = result;
    }
}
