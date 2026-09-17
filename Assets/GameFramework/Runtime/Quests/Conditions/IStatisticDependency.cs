using GameFramework.Progression.Statistics;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Internal capability letting <see cref="ConditionStatisticIndex"/> discover which
    /// <see cref="StatisticId"/> a condition depends on, without a type-check per concrete condition
    /// class. Implemented by <see cref="StatisticCondition"/> and <see cref="StatisticFlagCondition"/>.</summary>
    internal interface IStatisticDependency
    {
        StatisticId Statistic { get; }
    }
}
