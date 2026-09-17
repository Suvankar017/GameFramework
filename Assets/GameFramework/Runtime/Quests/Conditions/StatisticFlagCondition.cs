using GameFramework.Core.Validation;
using GameFramework.Progression.Statistics;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Checks a Boolean statistic against an expected value (e.g. "Complete tutorial" =
    /// TutorialCompleted == true). No progress concept applies here - <see cref="StatisticCondition"/>
    /// is the one to use for Integer thresholds.</summary>
    public sealed class StatisticFlagCondition : ICondition, IStatisticDependency
    {
        private readonly IStatisticsService _statistics;
        private readonly StatisticId _statistic;
        private readonly bool _expectedValue;

        public StatisticFlagCondition(IStatisticsService statistics, StatisticId statistic, bool expectedValue = true)
        {
            _statistics = Guard.NotNull(statistics, nameof(statistics));
            _statistic = statistic;
            _expectedValue = expectedValue;
        }

        StatisticId IStatisticDependency.Statistic => _statistic;

        public bool IsSatisfied() => _statistics.GetBool(_statistic) == _expectedValue;

        public string Describe() => $"{_statistic} == {_expectedValue}";
    }
}
