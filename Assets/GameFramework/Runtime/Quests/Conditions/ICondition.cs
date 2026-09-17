namespace GameFramework.Quests.Conditions
{
    /// <summary>
    /// Composable condition an objective (and, through it, a quest/achievement/milestone) depends
    /// on. Deliberately mirrors <see cref="GameFramework.Unlocks.IUnlockRequirement"/>'s shape —
    /// concrete conditions are constructed with direct references to whatever they need to check
    /// (see <see cref="StatisticCondition"/>, <see cref="LevelCondition"/>,
    /// <see cref="CurrencyCondition"/>), a pure function of already-injected state, so each is
    /// independently unit-testable without a live service registry. Kept as a separate interface
    /// from <see cref="GameFramework.Unlocks.IUnlockRequirement"/> rather than reusing it because
    /// this domain also needs progress reporting (<see cref="IProgressCondition"/>), which unlocking
    /// content has no use for.
    /// </summary>
    public interface ICondition
    {
        bool IsSatisfied();

        /// <summary>Human-readable explanation of what this condition needs — UI reads this instead
        /// of reimplementing the check itself.</summary>
        string Describe();
    }
}
