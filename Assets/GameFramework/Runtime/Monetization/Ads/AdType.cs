namespace GameFramework.Monetization.Ads
{
    /// <summary>The ad formats this framework's abstractions cover - see CLAUDE.md's Phase 15
    /// brief, section 6. Adding a future format is an additive enum value plus provider support,
    /// never a redesign of <see cref="IAdsService"/>/<see cref="IAdProvider"/>.</summary>
    public enum AdType
    {
        Banner,
        Interstitial,
        Rewarded
    }
}
