namespace GameFramework.Monetization.Entitlements
{
    /// <summary>Published by <see cref="EntitlementService"/> through the Phase 2 Event System
    /// whenever an entitlement is granted, revoked, or updated by <see cref="IEntitlementService.SyncFromProvider"/> -
    /// Phase 16 (Analytics) and a game's own ads/store UI are the intended listeners (see CLAUDE.md's
    /// Phase 15 brief, section 51).</summary>
    public readonly struct EntitlementChangedEvent
    {
        public readonly EntitlementId Id;
        public readonly bool IsOwned;

        public EntitlementChangedEvent(EntitlementId id, bool isOwned)
        {
            Id = id;
            IsOwned = isOwned;
        }
    }
}
