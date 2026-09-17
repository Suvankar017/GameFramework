namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Controls how much profiling/diagnostic overhead the framework pays at runtime. Set via
    /// <see cref="PerformanceSettings.Mode"/> - never hard-coded, so a production build can run
    /// <see cref="Disabled"/> while a QA build runs <see cref="Development"/> without a recompile.
    /// </summary>
    public enum ProfilingMode
    {
        /// <summary>No <see cref="ProfileScope"/> markers, no frame-timing sampling, no budget
        /// checks. The default for a release build.</summary>
        Disabled,

        /// <summary>Profiler markers and frame timing are active; budget warnings are logged
        /// (rate-limited). The recommended default for development/QA builds.</summary>
        Development,

        /// <summary>Everything <see cref="Development"/> does, plus per-category frame-time
        /// breakdowns are retained for the optional overlay/report. Slightly more overhead - use
        /// when actively investigating a specific performance issue, not as a permanent default.</summary>
        Detailed
    }
}
