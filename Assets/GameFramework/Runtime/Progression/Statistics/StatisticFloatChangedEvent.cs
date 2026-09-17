namespace GameFramework.Progression.Statistics
{
    /// <summary>Published by <see cref="StatisticsService"/> whenever a Float statistic's value
    /// actually changes. Kept separate from <see cref="StatisticChangedEvent"/> (rather than widening
    /// it) because Float statistics are the exception, not the default — see
    /// <see cref="StatisticValueType"/>'s remarks.</summary>
    public readonly struct StatisticFloatChangedEvent
    {
        public readonly StatisticId Statistic;
        public readonly float PreviousValue;
        public readonly float NewValue;
        public readonly string Reason;

        public StatisticFloatChangedEvent(StatisticId statistic, float previousValue, float newValue, string reason)
        {
            Statistic = statistic;
            PreviousValue = previousValue;
            NewValue = newValue;
            Reason = reason;
        }
    }
}
