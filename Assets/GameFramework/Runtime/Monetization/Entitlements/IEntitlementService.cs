using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Entitlements
{
    /// <summary>
    /// What the player currently owns, kept separate from the purchase transactions that granted it
    /// - see CLAUDE.md's Phase 15 brief, section 18. Game code (including ad policy - see
    /// <see cref="Ads.IAdsService"/>) asks <see cref="HasEntitlement"/> instead of inspecting raw
    /// purchase state.
    ///
    /// This service's data is a client-side cache (see CLAUDE.md's Phase 15 brief, section 50): it
    /// is not a substitute for server-side receipt validation, and the framework establishes no such
    /// backend in this phase.
    /// </summary>
    public interface IEntitlementService : IGameService
    {
        /// <summary>True only while <paramref name="id"/> is currently owned and, for a
        /// subscription, not expired - see <see cref="EntitlementState.IsCurrentlyActive"/>.</summary>
        bool HasEntitlement(EntitlementId id);

        /// <summary>Returns <see cref="EntitlementState.NotOwned"/> for an id with no record on file.</summary>
        EntitlementState GetEntitlement(EntitlementId id);

        IReadOnlyList<EntitlementId> OwnedEntitlements { get; }

        /// <summary>Grants or refreshes an entitlement outside the purchase flow (a promo code, a
        /// support/QA grant) or from <see cref="Purchases.IPurchaseService"/> when a purchase
        /// completes. <paramref name="expirationUtc"/> is null for a permanent entitlement.
        /// Publishes <see cref="EntitlementChangedEvent"/>.</summary>
        void GrantEntitlement(EntitlementId id, EntitlementSource source, DateTime? expirationUtc = null, bool isAutoRenewing = false);

        /// <summary>Revokes an entitlement (a refund, a lapsed subscription). A no-op if it is not
        /// currently owned. Publishes <see cref="EntitlementChangedEvent"/>.</summary>
        void RevokeEntitlement(EntitlementId id);

        /// <summary>
        /// Reconciles cached state against what a provider restore/receipt re-check reports - see
        /// CLAUDE.md's Phase 15 brief, section 25/26. Every entry in <paramref name="ownedEntitlements"/>
        /// is granted/refreshed with <see cref="EntitlementSource.Restored"/>; this does not revoke
        /// anything not present in the list (the provider not returning an id means "it did not
        /// report on it," not "the player no longer owns it" - a real store restore is
        /// non-exhaustive by nature, e.g. does not re-list already-consumed consumables).
        /// </summary>
        void SyncFromProvider(IReadOnlyList<EntitlementState> ownedEntitlements);

        void Save();
        void Load();

        event Action<EntitlementId, bool> EntitlementChanged;
    }
}
