using System.Collections.Generic;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>See CLAUDE.md's Phase 15 brief, section 26. <see cref="RestoredProductIds"/> drives
    /// which products <see cref="PurchaseService"/> (re-)grants entitlements/rewards for.</summary>
    public readonly struct RestoreResult
    {
        public readonly bool Success;
        public readonly IReadOnlyList<ProductId> RestoredProductIds;
        public readonly string FailureDetail;

        public RestoreResult(bool success, IReadOnlyList<ProductId> restoredProductIds, string failureDetail)
        {
            Success = success;
            RestoredProductIds = restoredProductIds ?? System.Array.Empty<ProductId>();
            FailureDetail = failureDetail;
        }
    }
}
