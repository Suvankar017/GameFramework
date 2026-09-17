using GameFramework.Runtime.Services;

namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Lightweight frame-timing diagnostics and configurable performance budgets - not a
    /// replacement for the Unity Profiler (see <see cref="ProfileScope"/> for that side of things).
    /// This answers "is a frame/operation over budget right now", not "why".
    /// </summary>
    public interface IPerformanceMonitorService : IGameService
    {
        /// <summary>Reference frame rate used to derive <see cref="FrameBudgetMilliseconds"/> - a
        /// configured target (e.g. 30 or 60), not a guarantee. See remarks on
        /// <see cref="PerformanceMonitorService"/> for what actually determines real performance.</summary>
        int TargetFrameRate { get; set; }

        /// <summary>1000 / <see cref="TargetFrameRate"/>. E.g. 33.33ms at 30 FPS, 16.67ms at 60 FPS.</summary>
        float FrameBudgetMilliseconds { get; }

        /// <summary>A frame whose real (unscaled) delta time exceeds this is counted as a spike.
        /// Defaults to <see cref="FrameBudgetMilliseconds"/> when not set explicitly.</summary>
        float SpikeThresholdMilliseconds { get; set; }

        /// <summary>Minimum real-time seconds between two spike log lines, so a sustained slow
        /// period logs once every interval rather than every single frame.</summary>
        float SpikeLogIntervalSeconds { get; set; }

        FrameTimeStats GetFrameStats();

        /// <summary>Resets the rolling average/worst/spike counters (not <see cref="TargetFrameRate"/>
        /// or registered budgets).</summary>
        void ResetFrameStats();

        /// <summary>Registers (or replaces) a named budget. Call once at startup per named budget -
        /// this is not meant to be re-registered every frame.</summary>
        void RegisterBudget(string name, float budgetMilliseconds);

        /// <summary>
        /// Reports how long an already-completed operation took, checked against a budget
        /// registered under the same <paramref name="name"/> via <see cref="RegisterBudget"/>. A
        /// no-op if no budget is registered under that name, or if <see cref="PerformanceSettings.IsProfilingEnabled"/>
        /// is false. Logs a rate-limited warning (once per <see cref="SpikeLogIntervalSeconds"/> per
        /// budget name) when exceeded - never every call.
        /// </summary>
        void ReportSample(string name, float elapsedMilliseconds);
    }
}
