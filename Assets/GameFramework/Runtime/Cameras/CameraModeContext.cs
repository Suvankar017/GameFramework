namespace GameFramework.Cameras
{
    /// <summary>Per-tick input handed to <see cref="ICameraMode.ComputePose"/> - deliberately just
    /// the target, not the whole <see cref="CameraController"/>, so a mode can never reach past its
    /// documented inputs into unrelated controller state.</summary>
    public readonly struct CameraModeContext
    {
        public readonly ICameraTarget Target;

        /// <summary>True only if <see cref="Target"/> is non-null and currently
        /// <see cref="ICameraTarget.IsValid"/> - a mode should treat a false value as "no target"
        /// (CLAUDE.md's Phase 11 brief, section 35) rather than null-checking itself.</summary>
        public readonly bool HasTarget;

        /// <summary>True while the player's "Camera.ReduceMotion" accessibility setting is on - a
        /// built-in mode must treat this as "skip damping/smoothing" (position/rotation still update
        /// correctly, just without the eased animation) rather than freezing entirely (CLAUDE.md's
        /// Phase 11 brief, section 29: "must allow disabling or reducing camera motion without
        /// breaking gameplay camera positioning").</summary>
        public readonly bool ReduceMotion;

        public CameraModeContext(ICameraTarget target, bool reduceMotion = false)
        {
            Target = target;
            HasTarget = target != null && target.IsValid;
            ReduceMotion = reduceMotion;
        }
    }
}
