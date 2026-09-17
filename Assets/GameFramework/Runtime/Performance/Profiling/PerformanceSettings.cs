namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Process-wide switch for how much profiling overhead <see cref="ProfileScope"/> and
    /// <see cref="PerformanceMonitorService"/> pay. A static field rather than a registered service
    /// deliberately: <see cref="ProfileScope"/> is constructed in hot paths (every pooled Get,
    /// every tick phase) and cannot afford a service-registry lookup just to check whether
    /// profiling is even on. Defaults to <see cref="ProfilingMode.Development"/> in the Editor and
    /// development builds, <see cref="ProfilingMode.Disabled"/> otherwise - set it explicitly at
    /// startup (e.g. from your composition root) if a game wants different behavior.
    /// </summary>
    public static class PerformanceSettings
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static ProfilingMode Mode = ProfilingMode.Development;
#else
        public static ProfilingMode Mode = ProfilingMode.Disabled;
#endif

        public static bool IsProfilingEnabled => Mode != ProfilingMode.Disabled;

        public static bool IsDetailed => Mode == ProfilingMode.Detailed;
    }
}
