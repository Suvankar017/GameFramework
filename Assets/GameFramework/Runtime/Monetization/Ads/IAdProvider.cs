using System;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// Provider seam behind <see cref="IAdsService"/> - see CLAUDE.md's Phase 15 brief, sections 3/28.
    /// A concrete SDK adapter (e.g. a future Google Mobile Ads provider) implements this and stays
    /// isolated in its own assembly; <see cref="Providers.Mock.MockAdProvider"/> is the only
    /// implementation this phase ships, since no ad SDK is installed in this project (see
    /// CLAUDE.md's Phase 15 brief, section 29).
    ///
    /// Every event below may be raised synchronously from within a call to <see cref="Load"/>/
    /// <see cref="Show"/> (as <see cref="Providers.Mock.MockAdProvider"/> does) or asynchronously from
    /// a real SDK's own callback - <see cref="AdsService"/> handles both. An adapter whose SDK
    /// callback does not already arrive on the main thread must marshal it there itself before
    /// raising these events (see CLAUDE.md's Phase 15 brief, section 35) - this interface makes no
    /// threading guarantee on the adapter's behalf.
    /// </summary>
    public interface IAdProvider
    {
        bool IsInitialized { get; }

        /// <summary>Idempotent - a call while already initializing/initialized should not restart
        /// initialization. Invokes <paramref name="onComplete"/> exactly once.</summary>
        void Initialize(Action<bool> onComplete);

        void Load(AdPlacementId placementId, AdType type, string providerUnitId);

        bool IsAdAvailable(AdPlacementId placementId, AdType type);

        void Show(AdPlacementId placementId, AdType type);

        /// <summary>Only meaningful for <see cref="AdType.Banner"/> - a no-op for other types.</summary>
        void Hide(AdPlacementId placementId, AdType type);

        event Action<AdPlacementId> AdLoaded;
        event Action<AdPlacementId, string> AdLoadFailed;
        event Action<AdPlacementId> AdShown;
        event Action<AdPlacementId, string> AdShowFailed;

        /// <summary>Raised once the ad is dismissed, for every ad type - always follows
        /// <see cref="AdShown"/> (or an <see cref="AdShowFailed"/> that still resulted in a
        /// dismissable state), never fired instead of it.</summary>
        event Action<AdPlacementId> AdClosed;

        /// <summary>Rewarded only - raised when the provider confirms the reward condition was
        /// actually satisfied, always before that placement's <see cref="AdClosed"/>. Never raised
        /// for a Banner/Interstitial.</summary>
        event Action<AdPlacementId> AdRewardEarned;
    }
}
