namespace GameFramework.Quests.Quests
{
    public enum QuestClaimResult
    {
        Success,
        InvalidId,
        NotCompleted,

        /// <summary>The quest has no <see cref="QuestDefinition.RewardId"/> assigned.</summary>
        NoReward,

        /// <summary>This is the idempotency guard — see <see cref="GameFramework.Rewards.RewardClaimResult.AlreadyClaimed"/>,
        /// which this delegates to.</summary>
        AlreadyClaimed,

        GrantFailed
    }
}
