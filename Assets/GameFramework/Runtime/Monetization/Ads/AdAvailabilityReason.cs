namespace GameFramework.Monetization.Ads
{
    /// <summary>Why <see cref="IAdsService.CanShow"/> answered the way it did - the framework's
    /// mechanism for "can this placement currently be shown," never a game-specific policy decision
    /// (see CLAUDE.md's Phase 15 brief, section 11).</summary>
    public enum AdAvailabilityReason
    {
        Available,
        NotInitialized,
        UnknownPlacement,
        NotLoaded,
        Cooldown,
        SessionLimitReached,
        SuppressedByEntitlement
    }
}
