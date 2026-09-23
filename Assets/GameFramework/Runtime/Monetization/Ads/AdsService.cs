using System;
using System.Collections.Generic;
using GameFramework.Monetization.Entitlements;
using GameFramework.Performance.Mobile;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// Default <see cref="IAdsService"/>, orchestrating one <see cref="IAdProvider"/> - see
    /// CLAUDE.md's Phase 15 brief, sections 3/6/7/11/13/20/21.
    ///
    /// Only one Interstitial/Rewarded ad may be "showing" at a time (tracked by
    /// <see cref="_currentlyShowingFullscreen"/>); Banners are independent of that slot since a
    /// banner is a persistent overlay rather than a takeover. Lifecycle: subscribes to
    /// <see cref="ApplicationPausedEvent"/>/<see cref="ApplicationResumedEvent"/> (published by
    /// Phase 5's <c>Performance.Mobile.IApplicationLifecycleService</c> when a game has that service
    /// registered) to hide active banners while backgrounded - see section 13. This is a soft,
    /// event-only integration: if Performance isn't registered, nothing publishes those events and
    /// this simply never fires, exactly like every other soft dependency in this framework.
    /// </summary>
    public sealed class AdsService : IAdsService
    {
        private const string LogCategory = "Monetization.Ads";

        private sealed class PlacementRuntime
        {
            public AdPlacementConfig Config;
            public int LoadRetryCount;
            public float LastShownRealtime = float.NegativeInfinity;
            public int SessionShowCount;
        }

        private readonly AdConfiguration _configuration;
        private readonly IAdProvider _provider;
        private readonly Dictionary<string, PlacementRuntime> _placements = new Dictionary<string, PlacementRuntime>(StringComparer.Ordinal);
        private readonly HashSet<string> _activeBanners = new HashSet<string>(StringComparer.Ordinal);

        private ITimeService _time;
        private ITimerService _timer;
        private IEventService _events;
        private ILoggingService _log;
        private IEntitlementService _entitlements;

        private AdPlacementId _currentlyShowingFullscreen;
        private bool _isShowingFullscreen;
        private AdPlacementId _pendingRewardedPlacement;
        private Action<RewardedAdResult> _pendingRewardedCallback;
        private bool _pendingRewardEarned;
        private string _lastError;

        public MonetizationProviderState State { get; private set; } = MonetizationProviderState.NotInitialized;

        public event Action<AdPlacementId> AdLoaded;
        public event Action<AdPlacementId, string> AdLoadFailed;
        public event Action<AdPlacementId> AdShown;
        public event Action<AdPlacementId, string> AdShowFailed;
        public event Action<AdPlacementId> AdClosed;
        public event Action<AdPlacementId> AdRewardEarned;

        public AdsService(AdConfiguration configuration, IAdProvider provider)
        {
            _configuration = configuration;
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            if (_configuration != null)
            {
                foreach (AdPlacementConfig placement in _configuration.Placements)
                {
                    if (placement.Id.IsValid)
                    {
                        _placements[placement.Id.Value] = new PlacementRuntime { Config = placement };
                    }
                }
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            _timer = registry.Get<ITimerService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            registry.TryGet(out _entitlements);

            _provider.AdLoaded += OnProviderAdLoaded;
            _provider.AdLoadFailed += OnProviderAdLoadFailed;
            _provider.AdShown += OnProviderAdShown;
            _provider.AdShowFailed += OnProviderAdShowFailed;
            _provider.AdClosed += OnProviderAdClosed;
            _provider.AdRewardEarned += OnProviderAdRewardEarned;

            _events.Subscribe<ApplicationPausedEvent>(OnApplicationPaused);
            _events.Subscribe<ApplicationResumedEvent>(OnApplicationResumed);

            State = MonetizationProviderState.Initializing;
            try
            {
                _provider.Initialize(success =>
                {
                    State = success ? MonetizationProviderState.Initialized : MonetizationProviderState.Failed;
                    if (!success)
                    {
                        _lastError = "Provider initialization failed.";
                        _log?.Log(LogLevel.Warning, LogCategory, "Ad provider failed to initialize.");
                    }
                });
            }
            catch (Exception exception)
            {
                // Provider failure isolation: an ad SDK failing to start leaves ads unavailable, it does
                // not fail this service's (or the framework's) startup.
                State = MonetizationProviderState.Failed;
                _lastError = "Ad provider threw during Initialize.";
                _log?.Log(LogLevel.Error, LogCategory, $"{_lastError} {exception.GetType().Name}: {exception.Message}");
            }
        }

        public void Shutdown()
        {
            _provider.AdLoaded -= OnProviderAdLoaded;
            _provider.AdLoadFailed -= OnProviderAdLoadFailed;
            _provider.AdShown -= OnProviderAdShown;
            _provider.AdShowFailed -= OnProviderAdShowFailed;
            _provider.AdClosed -= OnProviderAdClosed;
            _provider.AdRewardEarned -= OnProviderAdRewardEarned;
            _events.Unsubscribe<ApplicationPausedEvent>(OnApplicationPaused);
            _events.Unsubscribe<ApplicationResumedEvent>(OnApplicationResumed);
        }

        public bool IsAdAvailable(AdPlacementId placementId)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                return false;
            }

            return State == MonetizationProviderState.Initialized && _provider.IsAdAvailable(placementId, runtime.Config.Type);
        }

        public AdAvailabilityReason CanShow(AdPlacementId placementId)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                return AdAvailabilityReason.UnknownPlacement;
            }

            if (State != MonetizationProviderState.Initialized)
            {
                return AdAvailabilityReason.NotInitialized;
            }

            if (!_provider.IsAdAvailable(placementId, runtime.Config.Type))
            {
                return AdAvailabilityReason.NotLoaded;
            }

            if (runtime.Config.CooldownSeconds > 0f && _time.Realtime - runtime.LastShownRealtime < runtime.Config.CooldownSeconds)
            {
                return AdAvailabilityReason.Cooldown;
            }

            if (runtime.Config.SessionShowLimit > 0 && runtime.SessionShowCount >= runtime.Config.SessionShowLimit)
            {
                return AdAvailabilityReason.SessionLimitReached;
            }

            if (IsSuppressedByEntitlement(runtime.Config.Type))
            {
                return AdAvailabilityReason.SuppressedByEntitlement;
            }

            return AdAvailabilityReason.Available;
        }

        public void Load(AdPlacementId placementId)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Load('{placementId}') requested for an unregistered placement.");
                return;
            }

            if (State != MonetizationProviderState.Initialized)
            {
                return;
            }

            runtime.LoadRetryCount = 0;
            _provider.Load(placementId, runtime.Config.Type, runtime.Config.ResolvePlatformUnitId());
        }

        public AdShowResult Show(AdPlacementId placementId)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                return AdShowResult.NotAvailable;
            }

            if (runtime.Config.Type == AdType.Rewarded)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Show('{placementId}') called for a Rewarded placement - use ShowRewarded instead.");
                return AdShowResult.ProviderError;
            }

            // Checked before CanShow deliberately: a fullscreen ad already on screen is a structural
            // mutual-exclusion constraint, not a policy one (cooldown/session-limit/entitlement) -
            // it must win even if, say, the same placement would otherwise be back on cooldown the
            // instant it was shown. ShowRewarded applies the same ordering for the same reason.
            if (runtime.Config.Type == AdType.Interstitial && _isShowingFullscreen)
            {
                return AdShowResult.AlreadyShowing;
            }

            AdAvailabilityReason reason = CanShow(placementId);
            if (reason != AdAvailabilityReason.Available)
            {
                return MapUnavailable(reason);
            }

            if (runtime.Config.Type == AdType.Interstitial)
            {
                _isShowingFullscreen = true;
                _currentlyShowingFullscreen = placementId;
            }

            _provider.Show(placementId, runtime.Config.Type);
            return AdShowResult.Shown;
        }

        public void ShowRewarded(AdPlacementId placementId, Action<RewardedAdResult> onComplete)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime) || runtime.Config.Type != AdType.Rewarded)
            {
                onComplete?.Invoke(RewardedAdResult.NotAvailable);
                return;
            }

            if (_isShowingFullscreen)
            {
                onComplete?.Invoke(RewardedAdResult.AlreadyShowing);
                return;
            }

            AdAvailabilityReason reason = CanShow(placementId);
            if (reason != AdAvailabilityReason.Available)
            {
                onComplete?.Invoke(MapUnavailableRewarded(reason));
                return;
            }

            _isShowingFullscreen = true;
            _currentlyShowingFullscreen = placementId;
            _pendingRewardedPlacement = placementId;
            _pendingRewardedCallback = onComplete;
            _pendingRewardEarned = false;

            _provider.Show(placementId, AdType.Rewarded);
        }

        public void Hide(AdPlacementId placementId)
        {
            if (!_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime) || runtime.Config.Type != AdType.Banner)
            {
                return;
            }

            _provider.Hide(placementId, AdType.Banner);
            _activeBanners.Remove(placementId.Value);
        }

        public AdsDiagnostics GetDiagnostics()
        {
            var loaded = new List<AdPlacementId>();
            foreach (KeyValuePair<string, PlacementRuntime> pair in _placements)
            {
                var id = new AdPlacementId(pair.Key);
                if (_provider.IsAdAvailable(id, pair.Value.Config.Type))
                {
                    loaded.Add(id);
                }
            }

            var banners = new List<AdPlacementId>();
            foreach (string id in _activeBanners)
            {
                banners.Add(new AdPlacementId(id));
            }

            return new AdsDiagnostics(State, loaded, banners, _lastError);
        }

        private bool IsSuppressedByEntitlement(AdType type)
        {
            if (_entitlements == null || _configuration == null)
            {
                return false;
            }

            foreach (AdEntitlementSuppressionRule rule in _configuration.EntitlementSuppressions)
            {
                if (!rule.EntitlementId.IsValid || !_entitlements.HasEntitlement(rule.EntitlementId))
                {
                    continue;
                }

                foreach (AdType suppressed in rule.SuppressedTypes)
                {
                    if (suppressed == type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static AdShowResult MapUnavailable(AdAvailabilityReason reason)
        {
            switch (reason)
            {
                case AdAvailabilityReason.NotInitialized:
                    return AdShowResult.NotInitialized;
                case AdAvailabilityReason.Cooldown:
                case AdAvailabilityReason.SessionLimitReached:
                case AdAvailabilityReason.SuppressedByEntitlement:
                    return AdShowResult.SuppressedByPolicy;
                default:
                    return AdShowResult.NotAvailable;
            }
        }

        private static RewardedAdResult MapUnavailableRewarded(AdAvailabilityReason reason)
        {
            switch (reason)
            {
                case AdAvailabilityReason.NotInitialized:
                    return RewardedAdResult.NotInitialized;
                case AdAvailabilityReason.Cooldown:
                case AdAvailabilityReason.SessionLimitReached:
                case AdAvailabilityReason.SuppressedByEntitlement:
                    return RewardedAdResult.SuppressedByPolicy;
                default:
                    return RewardedAdResult.NotAvailable;
            }
        }

        private void OnProviderAdLoaded(AdPlacementId placementId)
        {
            if (_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                runtime.LoadRetryCount = 0;
            }

            AdLoaded?.Invoke(placementId);
            _events.Publish(new AdLoadedEvent(placementId));
        }

        private void OnProviderAdLoadFailed(AdPlacementId placementId, string detail)
        {
            _lastError = detail;
            AdLoadFailed?.Invoke(placementId, detail);
            _events.Publish(new AdLoadFailedEvent(placementId, detail));

            if (_configuration == null || !_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                return;
            }

            if (runtime.LoadRetryCount >= _configuration.MaxLoadRetries)
            {
                return;
            }

            runtime.LoadRetryCount++;
            float delay = _configuration.RetryBackoffSeconds * runtime.LoadRetryCount;
            _timer.StartDelay(delay, () => _provider.Load(placementId, runtime.Config.Type, runtime.Config.ResolvePlatformUnitId()));
        }

        private void OnProviderAdShown(AdPlacementId placementId)
        {
            if (_placements.TryGetValue(placementId.Value ?? string.Empty, out PlacementRuntime runtime))
            {
                runtime.LastShownRealtime = _time.Realtime;
                runtime.SessionShowCount++;

                if (runtime.Config.Type == AdType.Banner)
                {
                    _activeBanners.Add(placementId.Value);
                }
            }

            AdShown?.Invoke(placementId);
            _events.Publish(new AdShownEvent(placementId));
        }

        private void OnProviderAdShowFailed(AdPlacementId placementId, string detail)
        {
            _lastError = detail;

            if (placementId.Equals(_pendingRewardedPlacement) && _pendingRewardedCallback != null)
            {
                CompletePendingRewarded(RewardedAdResult.Failed);
            }

            if (placementId.Equals(_currentlyShowingFullscreen))
            {
                ClearFullscreenSlot();
            }

            AdShowFailed?.Invoke(placementId, detail);
            _events.Publish(new AdShowFailedEvent(placementId, detail));
        }

        private void OnProviderAdClosed(AdPlacementId placementId)
        {
            if (placementId.Equals(_pendingRewardedPlacement) && _pendingRewardedCallback != null)
            {
                CompletePendingRewarded(_pendingRewardEarned ? RewardedAdResult.RewardEarned : RewardedAdResult.ClosedWithoutReward);
            }

            if (placementId.Equals(_currentlyShowingFullscreen))
            {
                ClearFullscreenSlot();
            }

            AdClosed?.Invoke(placementId);
            _events.Publish(new AdClosedEvent(placementId));
        }

        private void OnProviderAdRewardEarned(AdPlacementId placementId)
        {
            if (placementId.Equals(_pendingRewardedPlacement))
            {
                _pendingRewardEarned = true;
            }

            AdRewardEarned?.Invoke(placementId);
            _events.Publish(new AdRewardEarnedEvent(placementId));
        }

        private void CompletePendingRewarded(RewardedAdResult result)
        {
            Action<RewardedAdResult> callback = _pendingRewardedCallback;
            _pendingRewardedCallback = null;
            _pendingRewardedPlacement = default;
            _pendingRewardEarned = false;
            callback?.Invoke(result);
        }

        private void ClearFullscreenSlot()
        {
            _isShowingFullscreen = false;
            _currentlyShowingFullscreen = default;
        }

        private void OnApplicationPaused(ApplicationPausedEvent evt)
        {
            foreach (string id in _activeBanners)
            {
                if (_placements.TryGetValue(id, out PlacementRuntime runtime))
                {
                    _provider.Hide(new AdPlacementId(id), runtime.Config.Type);
                }
            }
        }

        // A banner hidden for backgrounding is still tracked in _activeBanners (unlike an explicit
        // Hide() call, which removes it) - re-showing it here restores exactly what was on screen
        // before the app was backgrounded, without the game needing to re-request it.
        private void OnApplicationResumed(ApplicationResumedEvent evt)
        {
            foreach (string id in _activeBanners)
            {
                if (_placements.TryGetValue(id, out PlacementRuntime runtime))
                {
                    _provider.Show(new AdPlacementId(id), runtime.Config.Type);
                }
            }
        }
    }
}
