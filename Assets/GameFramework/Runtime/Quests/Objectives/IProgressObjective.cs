using GameFramework.Gameplay.Objectives;

namespace GameFramework.Quests.Objectives
{
    /// <summary>An <see cref="IObjective"/> that can report numeric progress toward completion.
    /// <see cref="ProgressNormalized"/> is always in [0, 1] even for a condition with no numeric
    /// notion of progress (reports 0 while active, 1 once completed).</summary>
    public interface IProgressObjective : IObjective
    {
        int CurrentValue { get; }
        int RequiredValue { get; }
        float ProgressNormalized { get; }
    }
}
