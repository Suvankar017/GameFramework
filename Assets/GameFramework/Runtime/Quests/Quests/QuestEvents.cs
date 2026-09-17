using GameFramework.Rewards;

namespace GameFramework.Quests.Quests
{
    /// <summary>Published by <see cref="QuestService"/> through the Phase 2 Event System — no
    /// separate notification mechanism is introduced for this.</summary>
    public readonly struct QuestAvailableEvent
    {
        public readonly QuestId Quest;
        public QuestAvailableEvent(QuestId quest) => Quest = quest;
    }

    public readonly struct QuestStartedEvent
    {
        public readonly QuestId Quest;
        public QuestStartedEvent(QuestId quest) => Quest = quest;
    }

    public readonly struct QuestObjectiveCompletedEvent
    {
        public readonly QuestId Quest;
        public readonly string ObjectiveId;
        public QuestObjectiveCompletedEvent(QuestId quest, string objectiveId)
        {
            Quest = quest;
            ObjectiveId = objectiveId;
        }
    }

    public readonly struct QuestCompletedEvent
    {
        public readonly QuestId Quest;
        public QuestCompletedEvent(QuestId quest) => Quest = quest;
    }

    public readonly struct QuestRewardClaimedEvent
    {
        public readonly QuestId Quest;
        public readonly RewardId Reward;
        public QuestRewardClaimedEvent(QuestId quest, RewardId reward)
        {
            Quest = quest;
            Reward = reward;
        }
    }
}
