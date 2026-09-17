using GameFramework.Core.Validation;
using GameFramework.Progression.Statistics;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Compares an Integer statistic against a required value — the primary building block
    /// for progression content ("Complete 5 races" = RacesCompleted &gt;= 5). Exposes progress
    /// directly from the live statistic, so UI never needs to duplicate this check.</summary>
    public sealed class StatisticCondition : IProgressCondition, IStatisticDependency
    {
        private readonly IStatisticsService _statistics;
        private readonly StatisticId _statistic;
        private readonly int _requiredValue;
        private readonly ComparisonOperator _comparison;

        public StatisticCondition(
            IStatisticsService statistics,
            StatisticId statistic,
            int requiredValue,
            ComparisonOperator comparison = ComparisonOperator.GreaterOrEqual)
        {
            _statistics = Guard.NotNull(statistics, nameof(statistics));
            _statistic = statistic;
            _requiredValue = requiredValue;
            _comparison = comparison;
        }

        StatisticId IStatisticDependency.Statistic => _statistic;

        public int CurrentValue => _statistics.Get(_statistic);

        public int RequiredValue => _requiredValue;

        public bool IsSatisfied() => Compare(CurrentValue, _requiredValue, _comparison);

        public string Describe() => $"{_statistic} {Symbol(_comparison)} {_requiredValue}";

        internal static bool Compare(int current, int required, ComparisonOperator comparison)
        {
            switch (comparison)
            {
                case ComparisonOperator.GreaterOrEqual: return current >= required;
                case ComparisonOperator.Equal: return current == required;
                case ComparisonOperator.LessOrEqual: return current <= required;
                case ComparisonOperator.Greater: return current > required;
                case ComparisonOperator.Less: return current < required;
                default: return false;
            }
        }

        internal static string Symbol(ComparisonOperator comparison)
        {
            switch (comparison)
            {
                case ComparisonOperator.GreaterOrEqual: return ">=";
                case ComparisonOperator.Equal: return "==";
                case ComparisonOperator.LessOrEqual: return "<=";
                case ComparisonOperator.Greater: return ">";
                case ComparisonOperator.Less: return "<";
                default: return "?";
            }
        }
    }
}
