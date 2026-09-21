namespace GameFramework.Cameras.Modes
{
    /// <summary>Holds whatever pose the controller was last snapped to - "static" means placing the
    /// <see cref="CameraController"/>'s own GameObject at the desired position/rotation in the scene
    /// (no target assigned), not a position baked into the shared, reusable
    /// <see cref="Configuration.CameraConfiguration"/> asset (CLAUDE.md's Phase 11 brief, section 8).</summary>
    internal sealed class StaticCameraMode : ICameraMode
    {
        public CameraPose ComputePose(CameraModeContext context, CameraPose previousPose, float deltaTime) => previousPose;
    }
}
