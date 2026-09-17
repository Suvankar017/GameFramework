namespace GameFramework.Quests.Milestones
{
    public enum MilestoneClaimResult
    {
        Success,
        InvalidId,
        NotReached,
        NoReward,

        /// <summary>This is the idempotency guard — see <see cref="GameFramework.Rewards.RewardClaimResult.AlreadyClaimed"/>,
        /// which this delegates to.</summary>
        AlreadyClaimed,

        GrantFailed
    }
}
