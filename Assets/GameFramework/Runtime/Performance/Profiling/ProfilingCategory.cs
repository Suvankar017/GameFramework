namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Named boundaries for <see cref="ProfileScope"/> markers. Deliberately a fixed, small set —
    /// categories exist so Unity Profiler / development logs can group related work, not so every
    /// method gets its own marker. Add a category only when a real, recurring boundary needs one.
    /// </summary>
    public enum ProfilingCategory
    {
        Framework,
        Gameplay,
        Input,
        UI,
        Audio,
        Spawning,
        Pooling,
        Physics,
        Rendering,
        Loading,
        Persistence
    }
}
