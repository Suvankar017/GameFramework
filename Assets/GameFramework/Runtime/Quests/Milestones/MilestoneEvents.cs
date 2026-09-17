using GameFramework.Rewards;

namespace GameFramework.Quests.Milestones
{
    /// <summary>Published by <see cref="MilestoneService"/> through the Phase 2 Event System.</summary>
    public readonly struct MilestoneReachedEvent
    {
        public readonly MilestoneId Milestone;
        public MilestoneReachedEvent(MilestoneId milestone) => Milestone = milestone;
    }

    public readonly struct MilestoneRewardClaimedEvent
    {
        public readonly MilestoneId Milestone;
        public readonly RewardId Reward;
        public MilestoneRewardClaimedEvent(MilestoneId milestone, RewardId reward)
        {
            Milestone = milestone;
            Reward = reward;
        }
    }
}
