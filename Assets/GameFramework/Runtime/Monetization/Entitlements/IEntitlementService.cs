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

        /// <summary>
        /// Phase 19: true only if a provider-backed flow - a completed/restored purchase through
        /// <c>PurchaseService</c>, or <see cref="SyncFromProvider"/> - reported this entitlement
        /// during the current process. <see cref="HasEntitlement"/> answers from the locally persisted
        /// cache, which is a convenience for offline/instant start and is <b>not</b> proof of payment
        /// (a local file can be edited); this answers "has the store confirmed it since launch?". It
        /// is never persisted, so no local data can make it true. Still client-side: a real guarantee
        /// needs server-side receipt validation behind <c>IPurchaseValidator</c>.
        /// </summary>
        bool IsVerifiedThisSession(EntitlementId id);

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
