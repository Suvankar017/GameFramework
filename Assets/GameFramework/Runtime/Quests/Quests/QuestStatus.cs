namespace GameFramework.Quests.Quests
{
    /// <summary>
    /// Quest lifecycle. Only <see cref="Active"/> and <see cref="Completed"/> are genuinely persisted
    /// per quest (see <see cref="QuestService"/>'s remarks) - <see cref="Locked"/>/<see cref="Available"/>
    /// are derived live from the quest's availability condition, and <see cref="Claimed"/> is derived
    /// from <see cref="GameFramework.Rewards.IRewardService.HasClaimed"/> so reward-claim idempotency
    /// is never tracked twice.
    /// </summary>
    public enum QuestStatus
    {
        Locked,
        Available,
        Active,
        Completed,
        Claimed
    }
}
