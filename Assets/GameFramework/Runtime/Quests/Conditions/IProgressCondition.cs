namespace GameFramework.Quests.Conditions
{
    /// <summary>A condition that can report "how close" it is to being satisfied as a simple
    /// current/required pair (e.g. 65/100 coins collected) — not every condition can express this
    /// meaningfully (a boolean flag condition has no useful progress), so this is opt-in rather than
    /// part of <see cref="ICondition"/> itself.</summary>
    public interface IProgressCondition : ICondition
    {
        int CurrentValue { get; }
        int RequiredValue { get; }
    }
}
