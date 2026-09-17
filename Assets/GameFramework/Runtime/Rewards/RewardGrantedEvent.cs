namespace GameFramework.Rewards
{
    /// <summary>Published every time a reward's contents are actually granted - including every
    /// time for a <see cref="RewardClaimPolicy.Repeatable"/> reward.</summary>
    public readonly struct RewardGrantedEvent
    {
        public readonly RewardId Reward;
        public readonly string Reason;

        public RewardGrantedEvent(RewardId reward, string reason)
        {
            Reward = reward;
            Reason = reason;
        }
    }
}
