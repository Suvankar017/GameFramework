using System;

namespace GameFramework.Monetization.Entitlements
{
    /// <summary>
    /// Provider-independent snapshot of what a player currently owns for one <see cref="EntitlementId"/>
    /// - see CLAUDE.md's Phase 15 brief, section 18 ("a purchase is an event/transaction; an
    /// entitlement is what the player currently owns"). <see cref="ExpirationUtc"/>/<see cref="IsAutoRenewing"/>
    /// are only meaningful for a subscription-backed entitlement; both are null/false for a
    /// permanent one.
    /// </summary>
    public readonly struct EntitlementState
    {
        public readonly EntitlementId Id;
        public readonly bool IsOwned;
        public readonly DateTime? ExpirationUtc;
        public readonly bool IsAutoRenewing;
        public readonly EntitlementSource Source;

        public EntitlementState(EntitlementId id, bool isOwned, DateTime? expirationUtc, bool isAutoRenewing, EntitlementSource source)
        {
            Id = id;
            IsOwned = isOwned;
            ExpirationUtc = expirationUtc;
            IsAutoRenewing = isAutoRenewing;
            Source = source;
        }

        /// <summary>Not owned, no record on file - the default for any id nothing has ever granted.</summary>
        public static EntitlementState NotOwned(EntitlementId id) =>
            new EntitlementState(id, false, null, false, EntitlementSource.Granted);

        /// <summary>True only while actually usable right now - <see cref="IsOwned"/> and, for a
        /// time-limited entitlement, not yet past <see cref="ExpirationUtc"/>. This is what
        /// <see cref="IEntitlementService.HasEntitlement"/> returns; <see cref="IsOwned"/> alone
        /// does not account for expiration.</summary>
        public bool IsCurrentlyActive(DateTime utcNow) =>
            IsOwned && (!ExpirationUtc.HasValue || ExpirationUtc.Value > utcNow);
    }
}
