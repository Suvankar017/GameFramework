namespace GameFramework.Cameras
{
    /// <summary>
    /// One camera behavior strategy (Follow/Static/TargetLook/Manual, or a game's own) - pure,
    /// Unity-lifecycle-free by convention (mirrors <c>Presentation.CameraShakeState</c>'s own
    /// "kept separate so it's unit-testable in EditMode" reasoning) so every built-in mode is
    /// testable without a live Camera or scene. New modes are added by implementing this interface,
    /// never by modifying <see cref="CameraController"/> or <see cref="ICameraService"/> (CLAUDE.md's
    /// Phase 11 brief, section 8).
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>Computes this frame's desired pose. <paramref name="previousPose"/> is the pose
        /// this same controller last produced (or its initial pose on the very first tick) - a mode
        /// with no target, or an invalid one, should normally return it unchanged rather than
        /// snapping to a default (CLAUDE.md's Phase 11 brief, section 35's "missing target"
        /// fallback).</summary>
        CameraPose ComputePose(CameraModeContext context, CameraPose previousPose, float deltaTime);
    }
}
