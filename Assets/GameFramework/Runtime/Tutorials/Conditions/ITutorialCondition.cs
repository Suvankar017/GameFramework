namespace GameFramework.Tutorials.Conditions
{
    /// <summary>
    /// Composable condition a <see cref="Steps.ConditionStep"/> waits on. Deliberately mirrors
    /// <see cref="Quests.Conditions.ICondition"/>'s shape (a pure function of already-injected
    /// state, independently unit-testable) but is a separate, local interface rather than a reuse
    /// of it - referencing <c>GameFramework.Quests</c> from this assembly would pull in Rewards/
    /// Unlocks/Progression as well, which CLAUDE.md's Phase 9 brief explicitly asks Tutorials to
    /// avoid (section 41) so the framework stays usable by a game that has none of those systems.
    /// The framework ships no concrete conditions beyond <see cref="DelegateTutorialCondition"/> -
    /// a game supplies its own (<c>CarStartedMovingCondition</c>, ...) by implementing this
    /// interface from outside this assembly.
    /// </summary>
    public interface ITutorialCondition
    {
        bool IsSatisfied();

        /// <summary>Human-readable explanation of what this condition needs - UI/diagnostics read
        /// this instead of reimplementing the check.</summary>
        string Describe();
    }
}
