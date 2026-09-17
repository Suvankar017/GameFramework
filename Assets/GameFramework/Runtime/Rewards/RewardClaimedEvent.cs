namespace GameFramework.Rewards
{
    /// <summary>Published only for a <see cref="RewardClaimPolicy.Once"/> reward, alongside
    /// <see cref="RewardGrantedEvent"/>, the moment its one-time claim is recorded — distinct from
    /// "granted" because "claimed" specifically means the idempotency flag is now set and this
    /// reward can never be granted again.</summary>
    public readonly struct RewardClaimedEvent
    {
        public readonly RewardId Reward;
        public readonly string Reason;

        public RewardClaimedEvent(RewardId reward, string reason)
        {
            Reward = reward;
            Reason = reason;
        }
    }
}
