namespace GameFramework.Cameras
{
    /// <summary>
    /// One outstanding temporary camera override, obtained from <see cref="ICameraService.PushOverride"/> -
    /// the same labeled-token shape as <c>GameFlow.IPauseToken</c> (CLAUDE.md's Phase 11 brief,
    /// section 26's "Boss Camera Override -&gt; Gameplay Camera"). Releasing a handle that is not
    /// currently the top of the override stack still removes it from wherever it sits, so nested
    /// overrides popped out of order (e.g. an Event Override released before a Pause Override that
    /// was pushed after it) never corrupt the stack - see <see cref="CameraService"/>'s remarks.
    /// </summary>
    public interface ICameraOverrideHandle
    {
        CameraId CameraId { get; }
        bool IsActive { get; }

        /// <summary>Releases this override. Safe to call more than once.</summary>
        void Release();
    }
}
