using System;
using System.Collections.Generic;
using GameFramework.Monetization.Ads;

namespace GameFramework.Monetization.Tests
{
    /// <summary>Fully controllable <see cref="IAdProvider"/> test double - unlike
    /// <see cref="Providers.Mock.MockAdProvider"/> (which resolves every call synchronously and is
    /// tested in its own right by <c>MockProviderTests</c>), this fake does nothing on its own; the
    /// test drives every event explicitly, which is what makes multi-step scenarios like
    /// "AlreadyShowing while a fullscreen ad is up" deterministic to set up.</summary>
    internal sealed class FakeAdProvider : IAdProvider
    {
        private readonly HashSet<string> _available = new HashSet<string>(StringComparer.Ordinal);

        public bool IsInitialized { get; private set; }
        public bool InitializeSucceeds { get; set; } = true;

        public List<AdPlacementId> LoadCalls { get; } = new List<AdPlacementId>();
        public List<AdPlacementId> ShowCalls { get; } = new List<AdPlacementId>();
        public List<AdPlacementId> HideCalls { get; } = new List<AdPlacementId>();

        public event Action<AdPlacementId> AdLoaded;
        public event Action<AdPlacementId, string> AdLoadFailed;
        public event Action<AdPlacementId> AdShown;
        public event Action<AdPlacementId, string> AdShowFailed;
        public event Action<AdPlacementId> AdClosed;
        public event Action<AdPlacementId> AdRewardEarned;

        public void Initialize(Action<bool> onComplete)
        {
            IsInitialized = true;
            onComplete?.Invoke(InitializeSucceeds);
        }

        public void Load(AdPlacementId placementId, AdType type, string providerUnitId) => LoadCalls.Add(placementId);

        public bool IsAdAvailable(AdPlacementId placementId, AdType type) => _available.Contains(placementId.Value ?? string.Empty);

        public void Show(AdPlacementId placementId, AdType type) => ShowCalls.Add(placementId);

        public void Hide(AdPlacementId placementId, AdType type) => HideCalls.Add(placementId);

        public void SetAvailable(AdPlacementId placementId, bool available)
        {
            if (available)
            {
                _available.Add(placementId.Value);
            }
            else
            {
                _available.Remove(placementId.Value);
            }
        }

        public void RaiseLoaded(AdPlacementId placementId)
        {
            SetAvailable(placementId, true);
            AdLoaded?.Invoke(placementId);
        }

        public void RaiseLoadFailed(AdPlacementId placementId, string detail) => AdLoadFailed?.Invoke(placementId, detail);
        public void RaiseShown(AdPlacementId placementId) => AdShown?.Invoke(placementId);
        public void RaiseShowFailed(AdPlacementId placementId, string detail) => AdShowFailed?.Invoke(placementId, detail);
        public void RaiseClosed(AdPlacementId placementId) => AdClosed?.Invoke(placementId);
        public void RaiseRewardEarned(AdPlacementId placementId) => AdRewardEarned?.Invoke(placementId);
    }
}
