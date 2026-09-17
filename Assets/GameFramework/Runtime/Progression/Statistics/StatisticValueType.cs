namespace GameFramework.Progression.Statistics
{
    /// <summary>The minimum useful value shape for a tracked statistic. Integer is the expected
    /// default for ordinary progression counters (races completed, coins collected, ...) — Float and
    /// Boolean exist for the genuinely different cases (a fractional measurement, a one-time flag)
    /// rather than as a general-purpose type system.</summary>
    public enum StatisticValueType
    {
        Integer,
        Float,
        Boolean
    }
}
