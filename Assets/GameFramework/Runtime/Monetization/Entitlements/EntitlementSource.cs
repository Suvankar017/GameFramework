namespace GameFramework.Monetization.Entitlements
{
    /// <summary>Where an <see cref="EntitlementState"/> currently on record came from - diagnostic/
    /// reconciliation context, not something gameplay code should need to branch on (branch on
    /// <see cref="IEntitlementService.HasEntitlement"/> instead).</summary>
    public enum EntitlementSource
    {
        /// <summary>Granted by a completed <see cref="Purchases.IPurchaseService"/> purchase.</summary>
        Purchase,

        /// <summary>Reinstated by <see cref="IEntitlementService.SyncFromProvider"/> (a store
        /// restore/receipt re-check), not a purchase made this session.</summary>
        Restored,

        /// <summary>Granted directly via <see cref="IEntitlementService.GrantEntitlement"/> outside
        /// the purchase flow (a promo code, a support/QA grant, cross-progression, ...).</summary>
        Granted
    }
}
