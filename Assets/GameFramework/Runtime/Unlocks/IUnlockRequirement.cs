namespace GameFramework.Unlocks
{
    /// <summary>
    /// Composable condition an unlock (or, via <see cref="PrerequisiteUnlockRequirement"/>, another
    /// unlock) depends on. Concrete requirements are constructed with direct references to whatever
    /// they need to check (see <see cref="LevelRequirement"/>, <see cref="CurrencyRequirement"/>,
    /// <see cref="ItemRequirement"/>) — a pure function of already-injected state, so each is
    /// independently unit-testable without a live service registry.
    /// </summary>
    public interface IUnlockRequirement
    {
        bool IsSatisfied();

        /// <summary>Human-readable explanation of what this requirement needs — UI reads this
        /// instead of reimplementing the check itself (see <see cref="UnlockResult"/>).</summary>
        string Describe();
    }
}
