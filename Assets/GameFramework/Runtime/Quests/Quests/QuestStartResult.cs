namespace GameFramework.Quests.Quests
{
    /// <summary>Outcome of <see cref="IQuestService.Start"/> — an expected gameplay failure is a
    /// normal result, never an exception.</summary>
    public enum QuestStartResult
    {
        Success,
        InvalidId,
        NotAvailable,
        AlreadyActive,
        AlreadyCompleted
    }
}
