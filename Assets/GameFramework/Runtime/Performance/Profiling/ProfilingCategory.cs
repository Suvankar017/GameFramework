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
        Persistence,

        /// <summary>Phase 10's <c>GameFramework.Presentation</c> orchestration boundary (feedback
        /// dispatch, camera shake, screen effects) - distinct from <see cref="Audio"/>/<see cref="UI"/>,
        /// which already cover their own lower-level playback/rendering work.</summary>
        Presentation,

        /// <summary>Phase 11's <c>GameFramework.Cameras</c> per-frame pose computation
        /// (<c>CameraDriver.LateUpdate</c>) - distinct from <see cref="Presentation"/>, which covers
        /// camera *feedback* (shake) layered on top of the base pose this category measures.</summary>
        Cameras
    }
}
