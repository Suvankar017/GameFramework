using System;
using System.Collections.Generic;
using GameFramework.Monetization.Ads;

namespace GameFramework.Monetization.Providers.Mock
{
    /// <summary>
    /// Deterministic <see cref="IAdProvider"/> for the Editor and automated tests - see CLAUDE.md's
    /// Phase 15 brief, sections 29/39/44. Every operation completes synchronously within the same
    /// call, which is deliberate: <see cref="Ads.AdsService"/> is written to handle both synchronous
    /// (this provider) and asynchronous (a real SDK adapter) event delivery identically.
    ///
    /// This is the framework's only shipped <see cref="IAdProvider"/> in this phase - no
    /// Google Mobile Ads (or other) adapter exists because that SDK is not installed in this project
    /// (see CLAUDE.md's Phase 15 brief, section 29 and this project's Phase 15 completion report for
    /// the exact seam a future adapter would fill).
    /// </summary>
    public sealed class MockAdProvider : IAdProvider
    {
        private readonly MockAdSimulationMode _mode;
        private readonly HashSet<string> _loadedPlacements = new HashSet<string>(StringComparer.Ordinal);

        public bool IsInitialized { get; private set; }

        public event Action<AdPlacementId> AdLoaded;
        public event Action<AdPlacementId, string> AdLoadFailed;
        public event Action<AdPlacementId> AdShown;
        public event Action<AdPlacementId, string> AdShowFailed;
        public event Action<AdPlacementId> AdClosed;
        public event Action<AdPlacementId> AdRewardEarned;

        public MockAdProvider(MockAdSimulationMode mode = MockAdSimulationMode.AlwaysSucceed)
        {
            _mode = mode;
        }

        public void Initialize(Action<bool> onComplete)
        {
            IsInitialized = true;
            onComplete?.Invoke(true);
        }

        public void Load(AdPlacementId placementId, AdType type, string providerUnitId)
        {
            if (_mode == MockAdSimulationMode.AlwaysFailToLoad)
            {
                AdLoadFailed?.Invoke(placementId, "Mock: simulated load failure.");
                return;
            }

            _loadedPlacements.Add(placementId.Value);
            AdLoaded?.Invoke(placementId);
        }

        public bool IsAdAvailable(AdPlacementId placementId, AdType type) =>
            _loadedPlacements.Contains(placementId.Value ?? string.Empty);

        public void Show(AdPlacementId placementId, AdType type)
        {
            // A real network consumes the loaded ad on show; this mock does the same so a game
            // relying on IsAdAvailable to gate a second Show sees an accurate answer.
            _loadedPlacements.Remove(placementId.Value);

            if (_mode == MockAdSimulationMode.AlwaysFailToShow)
            {
                AdShowFailed?.Invoke(placementId, "Mock: simulated show failure.");
                return;
            }

            AdShown?.Invoke(placementId);

            if (type == AdType.Banner)
            {
                // A banner is a persistent overlay, not a one-shot experience - it stays "shown"
                // until Hide() is called, unlike Interstitial/Rewarded below.
                return;
            }

            if (type == AdType.Rewarded)
            {
                if (_mode == MockAdSimulationMode.RewardedClosesWithoutReward)
                {
                    AdClosed?.Invoke(placementId);
                    return;
                }

                AdRewardEarned?.Invoke(placementId);
            }

            AdClosed?.Invoke(placementId);
        }

        public void Hide(AdPlacementId placementId, AdType type)
        {
            // No persistent banner view to tear down in the mock.
        }
    }
}
