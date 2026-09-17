namespace GameFramework.Quests.Achievements
{
    /// <summary>Read-only progress snapshot for one achievement.</summary>
    public readonly struct AchievementProgress
    {
        public readonly int CurrentValue;
        public readonly int RequiredValue;
        public readonly bool IsCompleted;

        public AchievementProgress(int currentValue, int requiredValue, bool isCompleted)
        {
            CurrentValue = currentValue;
            RequiredValue = requiredValue;
            IsCompleted = isCompleted;
        }
    }
}
