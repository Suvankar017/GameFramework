namespace GameFramework.Quests.Quests
{
    /// <summary>How many of a quest's objectives must complete for the quest itself to complete.</summary>
    public enum QuestCompletionRule
    {
        /// <summary>Every objective must complete.</summary>
        All,

        /// <summary>At least one objective must complete.</summary>
        Any,

        /// <summary>At least <see cref="QuestDefinition.RequiredObjectiveCount"/> objectives must
        /// complete.</summary>
        Count
    }
}
