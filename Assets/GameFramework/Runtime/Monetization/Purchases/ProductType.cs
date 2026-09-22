namespace GameFramework.Monetization.Purchases
{
    /// <summary>See CLAUDE.md's Phase 15 brief, section 14.</summary>
    public enum ProductType
    {
        /// <summary>Can be purchased any number of times (e.g. a coin pack) - typically maps to a
        /// <c>Rewards.RewardId</c> via <see cref="ProductDefinition.GrantedRewardId"/>.</summary>
        Consumable,

        /// <summary>Purchased once, owned permanently (e.g. Remove Ads) - typically maps to an
        /// <see cref="Entitlements.EntitlementId"/> via <see cref="ProductDefinition.GrantedEntitlementId"/>.</summary>
        NonConsumable,

        /// <summary>Recurring access - see CLAUDE.md's Phase 15 brief, section 27: only the fields a
        /// provider can reliably supply are represented (see <see cref="Entitlements.EntitlementState"/>),
        /// no subscription backend is implemented.</summary>
        Subscription
    }
}
