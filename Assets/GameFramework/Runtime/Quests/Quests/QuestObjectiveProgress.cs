namespace GameFramework.Quests.Quests
{
    /// <summary>Read-only snapshot of one objective's progress within a quest - what UI reads instead
    /// of reaching into the quest's internal objective instances.</summary>
    public readonly struct QuestObjectiveProgress
    {
        public readonly string ObjectiveId;
        public readonly string DisplayName;
        public readonly int CurrentValue;
        public readonly int RequiredValue;
        public readonly bool IsCompleted;

        public QuestObjectiveProgress(string objectiveId, string displayName, int currentValue, int requiredValue, bool isCompleted)
        {
            ObjectiveId = objectiveId;
            DisplayName = displayName;
            CurrentValue = currentValue;
            RequiredValue = requiredValue;
            IsCompleted = isCompleted;
        }
    }
}
