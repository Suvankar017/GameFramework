namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// A named, configurable time budget (e.g. "GameplayTick" at 8ms) a game registers with
    /// <see cref="PerformanceMonitorService"/>. These are reference budgets a game opts into, not
    /// universal guarantees the framework enforces - see the frame-rate note in
    /// <see cref="PerformanceMonitorService"/>'s remarks.
    /// </summary>
    public readonly struct PerformanceBudget
    {
        public readonly string Name;
        public readonly float BudgetMilliseconds;

        public PerformanceBudget(string name, float budgetMilliseconds)
        {
            Name = name;
            BudgetMilliseconds = budgetMilliseconds;
        }
    }
}
