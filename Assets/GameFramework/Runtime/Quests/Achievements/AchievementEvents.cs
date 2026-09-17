using GameFramework.Rewards;

namespace GameFramework.Quests.Achievements
{
    /// <summary>Published by <see cref="AchievementService"/> through the Phase 2 Event System.</summary>
    public readonly struct AchievementCompletedEvent
    {
        public readonly AchievementId Achievement;
        public AchievementCompletedEvent(AchievementId achievement) => Achievement = achievement;
    }

    public readonly struct AchievementRewardClaimedEvent
    {
        public readonly AchievementId Achievement;
        public readonly RewardId Reward;
        public AchievementRewardClaimedEvent(AchievementId achievement, RewardId reward)
        {
            Achievement = achievement;
            Reward = reward;
        }
    }
}
