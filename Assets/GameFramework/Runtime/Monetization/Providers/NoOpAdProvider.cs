using System;
using GameFramework.Monetization.Ads;

namespace GameFramework.Monetization.Providers
{
    /// <summary>
    /// Production-safe "no ad SDK installed" provider: initialization reports failure, so
    /// <see cref="AdsService"/> settles in <see cref="MonetizationProviderState.Failed"/> and every
    /// show request fails cleanly instead of pretending an ad played. This is what a release build gets
    /// when <see cref="MonetizationBootstrapper"/>'s mock toggle is left on (see
    /// <c>Runtime.Security.DevelopmentProviderGuard</c>) - never a simulated ad, never a reward.
    /// </summary>
    public sealed class NoOpAdProvider : IAdProvider
    {
        public bool IsInitialized => false;

        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(false);

        public void Load(AdPlacementId placementId, AdType type, string providerUnitId)
        {
        }

        public bool IsAdAvailable(AdPlacementId placementId, AdType type) => false;

        public void Show(AdPlacementId placementId, AdType type) =>
            AdShowFailed?.Invoke(placementId, "No ad provider is installed.");

        public void Hide(AdPlacementId placementId, AdType type)
        {
        }

        // Events required by the interface; only AdShowFailed is ever raised.
#pragma warning disable 0067
        public event Action<AdPlacementId> AdLoaded;
        public event Action<AdPlacementId, string> AdLoadFailed;
        public event Action<AdPlacementId> AdShown;
        public event Action<AdPlacementId> AdClosed;
        public event Action<AdPlacementId> AdRewardEarned;
#pragma warning restore 0067
        public event Action<AdPlacementId, string> AdShowFailed;
    }
}
