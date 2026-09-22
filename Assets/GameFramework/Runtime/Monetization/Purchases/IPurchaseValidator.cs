namespace GameFramework.Monetization.Purchases
{
    public readonly struct PurchaseValidationResult
    {
        public readonly bool IsValid;
        public readonly string FailureDetail;

        private PurchaseValidationResult(bool isValid, string failureDetail)
        {
            IsValid = isValid;
            FailureDetail = failureDetail;
        }

        public static PurchaseValidationResult Valid() => new PurchaseValidationResult(true, null);
        public static PurchaseValidationResult Invalid(string detail) => new PurchaseValidationResult(false, detail);
    }

    /// <summary>
    /// The purchase validation boundary - see CLAUDE.md's Phase 15 brief, section 22/50. This phase
    /// establishes the seam only; <see cref="LocalPurchaseValidator"/> is a trivial client-side check,
    /// explicitly NOT a substitute for server-side receipt validation. A game (or a later phase) that
    /// needs real security implements this interface against a backend and passes it to
    /// <see cref="PurchaseService"/>'s constructor.
    /// </summary>
    public interface IPurchaseValidator
    {
        PurchaseValidationResult Validate(PurchaseResult result, ProductDefinition product);
    }
}
