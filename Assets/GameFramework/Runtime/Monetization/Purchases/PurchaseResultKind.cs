namespace GameFramework.Monetization.Purchases
{
    /// <summary>See CLAUDE.md's Phase 15 brief, section 17/36 - a strongly typed outcome instead of
    /// a bare boolean.</summary>
    public enum PurchaseResultKind
    {
        Success,
        Cancelled,
        Failed,

        /// <summary>Awaiting further resolution (e.g. parental approval) - a later
        /// <see cref="IPurchaseProvider.PurchaseUpdated"/> reports the final outcome.</summary>
        Pending,

        /// <summary>Provider-approved but deferred (platform-specific, e.g. Ask to Buy) - same
        /// "wait for a later update" semantics as <see cref="Pending"/>.</summary>
        Deferred,

        AlreadyOwned,
        Restored,
        NotAvailable,
        NotInitialized,
        ProductUnavailable,
        ValidationFailed
    }
}
