using GameFramework.Rewards;

namespace GameFramework.Monetization.Tests
{
    /// <summary>Minimal <see cref="IReward"/> test double - counts grants without needing a real
    /// Economy/Inventory/Experience/Unlocks setup, since these tests are about
    /// Purchases/Ads-to-Reward wiring, not Phase 6 content itself (already covered by
    /// <c>Rewards.Tests</c>).</summary>
    internal sealed class CountingReward : IReward
    {
        public int GrantCount { get; private set; }
        public bool CanGrantResult { get; set; } = true;

        public bool CanGrant() => CanGrantResult;

        public RewardGrantResult Grant()
        {
            GrantCount++;
            return RewardGrantResult.Ok();
        }
    }
}
