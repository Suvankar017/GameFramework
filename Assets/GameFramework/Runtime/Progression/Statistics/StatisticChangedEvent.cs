namespace GameFramework.Progression.Statistics
{
    /// <summary>Published by <see cref="StatisticsService"/> through the Phase 2 Event System
    /// whenever an Integer or Boolean statistic's value actually changes (a no-op Set/Increment, or
    /// one rejected by validation/monotonic protection, never publishes). Boolean statistics report
    /// through this same event using 0/1 so a single subscription covers both without a second event
    /// type.</summary>
    public readonly struct StatisticChangedEvent
    {
        public readonly StatisticId Statistic;
        public readonly int PreviousValue;
        public readonly int NewValue;
        public readonly int Delta;
        public readonly string Reason;

        public StatisticChangedEvent(StatisticId statistic, int previousValue, int newValue, string reason)
        {
            Statistic = statistic;
            PreviousValue = previousValue;
            NewValue = newValue;
            Delta = newValue - previousValue;
            Reason = reason;
        }
    }
}
