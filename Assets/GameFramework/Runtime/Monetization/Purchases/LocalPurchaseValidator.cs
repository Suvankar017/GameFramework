namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Default <see cref="IPurchaseValidator"/> - accepts whatever outcome the provider itself
    /// reported (<see cref="PurchaseResultKind.Success"/>/<see cref="PurchaseResultKind.Restored"/>
    /// are valid, everything else is not). This is NOT secure purchase validation - see
    /// <see cref="IPurchaseValidator"/>'s remarks and CLAUDE.md's Phase 15 brief, section 50: a
    /// client can be tampered with, so this only guards against an obviously-wrong local state
    /// (e.g. a provider bug reporting Success with no transaction id), not fraud.
    /// </summary>
    public sealed class LocalPurchaseValidator : IPurchaseValidator
    {
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

            return PurchaseValidationResult.Valid();
        }
    }
}
