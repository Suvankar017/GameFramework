namespace GameFramework.Monetization.Purchases
{
    public readonly struct PurchaseResult
    {
        public readonly PurchaseResultKind Kind;
        public readonly ProductId ProductId;

        /// <summary>Provider transaction id, empty for a result with no underlying transaction
        /// (e.g. <see cref="PurchaseResultKind.ProductUnavailable"/>) - the key
        /// <see cref="PurchaseService"/> uses for idempotent reward/entitlement granting (see
        /// CLAUDE.md's Phase 15 brief, section 41).</summary>
        public readonly string TransactionId;

        public readonly string FailureDetail;

        public PurchaseResult(PurchaseResultKind kind, ProductId productId, string transactionId, string failureDetail)
        {
            Kind = kind;
            ProductId = productId;
            TransactionId = transactionId;
            FailureDetail = failureDetail;
        }

        public static PurchaseResult Immediate(PurchaseResultKind kind, ProductId productId, string failureDetail = null) =>
            new PurchaseResult(kind, productId, string.Empty, failureDetail);
    }
}
