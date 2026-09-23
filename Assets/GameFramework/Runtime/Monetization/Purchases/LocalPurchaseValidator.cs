namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Default <see cref="IPurchaseValidator"/> - accepts whatever outcome the provider itself
    /// reported (<see cref="PurchaseResultKind.Success"/>/<see cref="PurchaseResultKind.Restored"/>
    /// are valid, everything else is not). This is NOT secure purchase validation - see
    /// <see cref="IPurchaseValidator"/>'s remarks and CLAUDE.md's Phase 15 brief, section 50: a
    /// client can be tampered with, so this only guards against an obviously-wrong local state
    /// (e.g. a provider bug reporting Success with no transaction id, or a result for a different
    /// product than the one being granted), not fraud.
    /// </summary>
    public sealed class LocalPurchaseValidator : IPurchaseValidator
    {
        /// <summary>Structural sanity bound on an untrusted provider string. Real store transaction
        /// ids are far shorter; this only rejects obviously malformed input before it is persisted
        /// into <c>PurchaseSaveData</c>.</summary>
        public const int MaxTransactionIdLength = 512;

        public PurchaseValidationResult Validate(PurchaseResult result, ProductDefinition product)
        {
            if (result.Kind != PurchaseResultKind.Success && result.Kind != PurchaseResultKind.Restored)
            {
                return PurchaseValidationResult.Invalid($"Purchase result was {result.Kind}, not Success/Restored.");
            }

            if (string.IsNullOrEmpty(result.TransactionId))
            {
                return PurchaseValidationResult.Invalid("Purchase reported Success/Restored with no transaction id.");
            }

            if (result.TransactionId.Length > MaxTransactionIdLength)
            {
                return PurchaseValidationResult.Invalid($"Transaction id exceeds {MaxTransactionIdLength} characters.");
            }

            // Phase 19: a provider result must be for the product it is being applied to - never grant
            // product A's entitlement/reward because a callback reported product B.
            if (product == null || result.ProductId != product.Id)
            {
                return PurchaseValidationResult.Invalid("Purchase result's product id does not match the product being granted.");
            }

            return PurchaseValidationResult.Valid();
        }
    }
}
