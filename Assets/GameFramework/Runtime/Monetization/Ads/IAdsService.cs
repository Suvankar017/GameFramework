using System;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// Game-facing advertising API - see CLAUDE.md's Phase 15 brief, section 6. Game code depends
    /// only on this interface, never on a provider SDK (section 31).
    ///
    /// Initialization happens once, automatically, when this service is initialized by the
    /// composition root (<see cref="Monetization.MonetizationBootstrapper"/>) - see section 32; there
    /// is no public <c>Initialize()</c> a UI screen is expected to call.
    ///
    /// This service never grants a reward itself, including for a rewarded ad (see section 8): it
    /// only reports <see cref="RewardedAdResult.RewardEarned"/>/<see cref="AdRewardEarned"/>. Reward
    /// granting is the caller's responsibility (typically via <c>Rewards.IRewardService.TryClaim</c>),
    /// optionally through <see cref="Integration.AdPlacementRewardBridge"/>.
    /// </summary>
    public interface IAdsService : IGameService
    {
        MonetizationProviderState State { get; }

        bool IsAdAvailable(AdPlacementId placementId);

        /// <summary>The mechanism-level answer to "can this placement currently be shown" (loaded,
        /// not on cooldown, under its session limit, not suppressed by an owned entitlement) - the
        /// game still decides *when* to call <see cref="Show"/>/<see cref="ShowRewarded"/> (see
        /// CLAUDE.md's Phase 15 brief, section 11).</summary>
        AdAvailabilityReason CanShow(AdPlacementId placementId);

        /// <summary>Requests <paramref name="placementId"/> be loaded from the provider. Safe to call
        /// again while already loaded (a no-op) or loading. Publishes <see cref="AdLoadedEvent"/>/
        /// <see cref="AdLoadFailedEvent"/>, retrying up to <see cref="AdConfiguration.MaxLoadRetries"/>
        /// times on failure.</summary>
        void Load(AdPlacementId placementId);

        /// <summary>For Banner/Interstitial placements. Rewarded placements must go through
        /// <see cref="ShowRewarded"/> instead, which enforces the reward-only-on-completion
        /// guarantee.</summary>
        AdShowResult Show(AdPlacementId placementId);

        /// <summary>
        /// Shows a Rewarded placement. <paramref name="onComplete"/> is invoked exactly once, with
        /// <see cref="RewardedAdResult.RewardEarned"/> only when the provider actually confirmed the
        /// reward condition - never merely because this method was called (see CLAUDE.md's Phase 15
        /// brief, section 7). Invoked synchronously for an immediate rejection (e.g. not available).
        /// </summary>
        void ShowRewarded(AdPlacementId placementId, Action<RewardedAdResult> onComplete);

        /// <summary>Hides a currently shown Banner. A no-op for any other ad type or a placement
        /// that isn't currently shown.</summary>
        void Hide(AdPlacementId placementId);

        AdsDiagnostics GetDiagnostics();

        event Action<AdPlacementId> AdLoaded;
        event Action<AdPlacementId, string> AdLoadFailed;
        event Action<AdPlacementId> AdShown;
        event Action<AdPlacementId, string> AdShowFailed;
        event Action<AdPlacementId> AdClosed;
        event Action<AdPlacementId> AdRewardEarned;
    }
}
