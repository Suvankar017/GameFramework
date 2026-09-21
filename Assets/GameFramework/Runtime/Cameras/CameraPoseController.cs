using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Pure per-controller pipeline: <see cref="ICameraMode"/> computes a base position/rotation,
    /// <see cref="CameraZoomController"/> advances the damped zoom value, and
    /// <see cref="CameraBoundsConstraint"/> clamps the result - CLAUDE.md's Phase 11 brief, section
    /// 17's "Base Pose -&gt; Constraints -&gt; (Feedback, owned by Phase 10, applied later by
    /// <see cref="CameraDriver"/>'s sibling <c>Presentation.CameraFeedbackDriver</c>) -&gt; Final
    /// Pose". Owned by <see cref="CameraController"/>, kept Unity-lifecycle-free so the whole
    /// pipeline is testable without a live Camera or GameObject.
    /// </summary>
    internal sealed class CameraPoseController
    {
        private ICameraMode _mode;
        private ICameraTarget _target;
        private CameraBoundsSettings _bounds;
        private CameraZoomController _zoom;
        private bool _orthographic;
        private float _aspect = 1f;
        private CameraPose _pose;

        public CameraPose CurrentPose => _pose;

        public void Initialize(ICameraMode mode, CameraZoomSettings zoomSettings, CameraBoundsSettings bounds, bool orthographic, float aspect, CameraPose initialPose)
        {
            _mode = mode;
            _bounds = bounds;
            _orthographic = orthographic;
            _aspect = aspect;
            _pose = initialPose;
            _zoom = new CameraZoomController(zoomSettings, orthographic, orthographic ? initialPose.OrthographicSize : initialPose.FieldOfView);
        }

        public void SetMode(ICameraMode mode) => _mode = mode;
        public ICameraMode Mode => _mode;

        public void SetTarget(ICameraTarget target) => _target = target;
        public ICameraTarget Target => _target;

        public void SetAspect(float aspect) => _aspect = aspect;

        public void SetZoom(float value) => _zoom?.SetZoom(value);

        public CameraPose Tick(float deltaTime, bool reduceMotion = false)
        {
            var context = new CameraModeContext(_target, reduceMotion);
            CameraPose computed = _mode != null ? _mode.ComputePose(context, _pose, deltaTime) : _pose;

            float zoomValue = _zoom != null ? _zoom.Tick(deltaTime, reduceMotion) : (_orthographic ? computed.OrthographicSize : computed.FieldOfView);
            computed = _orthographic
                ? new CameraPose(computed.Position, computed.Rotation, zoomValue, computed.FieldOfView)
                : new CameraPose(computed.Position, computed.Rotation, computed.OrthographicSize, zoomValue);

            Vector3 clamped = CameraBoundsConstraint.Clamp(computed.Position, _bounds, _orthographic, zoomValue, _aspect);
            computed = new CameraPose(clamped, computed.Rotation, computed.OrthographicSize, computed.FieldOfView);

            _pose = computed;
            return computed;
        }

        /// <summary>Forces this controller's pose directly to whatever its mode/target currently
        /// resolve to, with no damping - used by <see cref="CameraController.Snap"/> for restart/
        /// respawn/recovery (CLAUDE.md's Phase 11 brief, section 27). Implemented as one
        /// <c>reduceMotion</c> tick (mirrors the accessibility path exactly, rather than
        /// reimplementing each mode's axis/dead-zone rules a second time here) so it stays correct
        /// for every mode, including a fixed (non-followed) axis, which must resolve from whatever
        /// this controller's pose already is - never from the target's own position on that axis
        /// (see CLAUDE.md's Phase 11 brief, section 10's "Static"/fixed-axis note).</summary>
        public void Snap()
        {
            Tick(0f, reduceMotion: true);
            _zoom?.Snap();
        }
    }
}
