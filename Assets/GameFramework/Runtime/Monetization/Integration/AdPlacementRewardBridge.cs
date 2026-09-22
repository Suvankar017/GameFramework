using System;
using System.Collections.Generic;
using GameFramework.Monetization.Ads;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Integration
{
    /// <summary>
    /// Optional, explicitly opt-in adapter mapping a rewarded ad placement to a
    /// <see cref="Rewards.RewardId"/> claimed through the existing <see cref="IRewardService"/> -
    /// see CLAUDE.md's Phase 15 brief, section 8: <see cref="Ads.IAdsService"/> itself never grants a
    /// reward, so a game that wants "watching this placement claims that reward" automatically
    /// creates one of these rather than hand-wiring <see cref="Ads.IAdsService.AdRewardEarned"/> in
    /// its own code (though hand-wiring remains just as valid - this class exists only to cover the
    /// common 1:1 case with no boilerplate).
    ///
    /// Deliberately not registered by <see cref="MonetizationBootstrapper"/> - unlike
    /// <see cref="Purchases.PurchaseService"/>'s product-to-reward grant (which is unconditional,
    /// authored data), whether a rewarded placement should auto-claim a reward at all is a
    /// game-specific policy decision (see section 55).
    /// </summary>
    public sealed class AdPlacementRewardBridge : IDisposable
    {
        private readonly IEventService _events;
        private readonly IRewardService _rewards;
        private readonly Dictionary<string, RewardId> _mapping;

        public AdPlacementRewardBridge(IServiceRegistry registry, IReadOnlyDictionary<AdPlacementId, RewardId> placementToReward)
        {
            _events = registry.Get<IEventService>();
            _rewards = registry.Get<IRewardService>();

            _mapping = new Dictionary<string, RewardId>(StringComparer.Ordinal);
            foreach (KeyValuePair<AdPlacementId, RewardId> pair in placementToReward)
            {
                if (pair.Key.IsValid)
                {
                    _mapping[pair.Key.Value] = pair.Value;
                }
            }

            _events.Subscribe<AdRewardEarnedEvent>(OnAdRewardEarned);
        }

        public void Dispose()
        {
            _events.Unsubscribe<AdRewardEarnedEvent>(OnAdRewardEarned);
        }

        private void OnAdRewardEarned(AdRewardEarnedEvent evt)
        {
            if (_mapping.TryGetValue(evt.PlacementId.Value ?? string.Empty, out RewardId rewardId))
            {
                _rewards.TryClaim(rewardId, "RewardedAd:" + evt.PlacementId);
            }
        }
    }
}
