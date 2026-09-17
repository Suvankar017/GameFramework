namespace GameFramework.Quests.Quests
{
    /// <summary>Whether a completed quest can be started again via <see cref="IQuestService.TryReset"/>.</summary>
    public enum QuestRepeatPolicy
    {
        OneTime,
        Repeatable,

        /// <summary>Repeatable up to <see cref="QuestDefinition.MaxRepeats"/> times.</summary>
        LimitedRepeats
    }
}
