using System.Collections.Generic;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Internal capability letting <see cref="ConditionStatisticIndex"/> and quest/achievement
    /// cycle-detection walk into <see cref="AllCondition"/>/<see cref="AnyCondition"/>/
    /// <see cref="NotCondition"/> trees.</summary>
    internal interface ICompositeCondition
    {
        IReadOnlyList<ICondition> Children { get; }
    }
}
