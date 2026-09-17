namespace GameFramework.Unlocks
{
    /// <summary>Outcome of <see cref="IUnlockService.TryUnlock"/> — an expected gameplay failure
    /// (requirement not met) is a normal result, never an exception. UI reads this instead of
    /// reimplementing the requirement check itself.</summary>
    public enum UnlockResult
    {
        Success,
        AlreadyUnlocked,
        RequirementNotMet,
        InvalidId
    }
}
