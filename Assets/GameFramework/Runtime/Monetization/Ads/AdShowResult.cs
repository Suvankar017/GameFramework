namespace GameFramework.Monetization.Ads
{
    /// <summary>Immediate outcome of <see cref="IAdsService.Show"/> for a Banner/Interstitial
    /// placement - the ad's actual load/display is still reported asynchronously via
    /// <see cref="IAdsService"/>'s events, this is only whether the show request itself was
    /// accepted.</summary>
    public enum AdShowResult
    {
        Shown,
        NotAvailable,
        NotInitialized,
        AlreadyShowing,
        SuppressedByPolicy,
        ProviderError
    }
}
