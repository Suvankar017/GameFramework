namespace GameFramework.Quests.Achievements
{
    public enum AchievementClaimResult
    {
        Success,
        InvalidId,
        NotCompleted,
        NoReward,

        /// <summary>This is the idempotency guard — see <see cref="GameFramework.Rewards.RewardClaimResult.AlreadyClaimed"/>,
        /// which this delegates to.</summary>
        AlreadyClaimed,

        GrantFailed
    }
}
