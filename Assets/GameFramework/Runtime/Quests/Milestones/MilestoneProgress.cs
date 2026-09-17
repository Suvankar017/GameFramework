namespace GameFramework.Quests.Milestones
{
    /// <summary>Read-only progress snapshot for one milestone.</summary>
    public readonly struct MilestoneProgress
    {
        public readonly int CurrentValue;
        public readonly int RequiredValue;
        public readonly bool IsReached;

        public MilestoneProgress(int currentValue, int requiredValue, bool isReached)
        {
            CurrentValue = currentValue;
            RequiredValue = requiredValue;
            IsReached = isReached;
        }
    }
}
