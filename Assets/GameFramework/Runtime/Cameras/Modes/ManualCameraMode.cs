namespace GameFramework.Cameras.Modes
{
    /// <summary>Passes through whatever pose an external system last supplied via
    /// <see cref="CameraController.SetManualPose"/> - lets that system provide camera intent
    /// without ever touching <see cref="UnityEngine.Camera"/>/<see cref="UnityEngine.Transform"/>
    /// directly (CLAUDE.md's Phase 11 brief, section 8). Until the first call, holds the previous
    /// pose unchanged.</summary>
    internal sealed class ManualCameraMode : ICameraMode
    {
        private CameraPose? _pose;

        public void SetPose(CameraPose pose) => _pose = pose;

        public CameraPose ComputePose(CameraModeContext context, CameraPose previousPose, float deltaTime) =>
            _pose ?? previousPose;
    }
}
