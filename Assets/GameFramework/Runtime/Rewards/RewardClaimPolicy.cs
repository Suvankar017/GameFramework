namespace GameFramework.Rewards
{
    /// <summary>How many times a reward may be claimed. The framework does not hard-code
    /// "level completion reward" or "daily reward" specifically - a game builds those on top of
    /// whichever policy fits (e.g. a daily reward re-registers/re-keys itself per day using
    /// <see cref="Once"/> under a day-qualified <see cref="RewardId"/>).</summary>
    public enum RewardClaimPolicy
    {
        /// <summary>Can be claimed exactly once, ever (until <see cref="IRewardService.ResetToDefaults"/>).</summary>
        Once,

        /// <summary>Can be claimed any number of times - no claim state is tracked or persisted.</summary>
        Repeatable
    }
}
