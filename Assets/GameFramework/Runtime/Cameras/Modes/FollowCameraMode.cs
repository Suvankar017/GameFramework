using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras.Modes
{
    /// <summary>
    /// Tracks <see cref="CameraModeContext.Target"/> subject to a per-axis follow mask, an optional
    /// dead/soft zone, and damping (CLAUDE.md's Phase 11 brief, sections 10-12). A missing or
    /// destroyed target holds the previous pose unchanged rather than snapping anywhere (section 35).
    ///
    /// The dead zone is evaluated relative to the camera's own current center, not the target's
    /// absolute position - the target can move freely within it; the camera only moves the minimum
    /// amount needed to keep the target at the zone's edge once it exits (section 11's diagram).
    /// Axes outside <see cref="CameraFollowSettings.FollowAxisMask"/> are left exactly where they
    /// were last snapped to - CLAUDE.md's Phase 11 brief, section 8's "Static" mode note applies
    /// equally to a fixed follow axis.
    /// </summary>
    internal sealed class FollowCameraMode : ICameraMode
    {
        private readonly CameraFollowSettings _settings;
        private Vector3 _dampVelocity;

        public FollowCameraMode(CameraFollowSettings settings)
        {
            _settings = settings;
        }

        public CameraPose ComputePose(CameraModeContext context, CameraPose previousPose, float deltaTime)
        {
            if (!context.HasTarget)
            {
                return previousPose;
            }

            Vector3 targetPosition = context.Target.Position + _settings.Offset;
            Vector3 offsetFromCamera = targetPosition - previousPose.Position;

            float moveX = CameraDeadZone.ClampAxis(offsetFromCamera.x, _settings.DeadZoneSize.x * 0.5f,
                _settings.UseSoftZone ? _settings.SoftZoneSize.x * 0.5f : 0f);
            float moveY = CameraDeadZone.ClampAxis(offsetFromCamera.y, _settings.DeadZoneSize.y * 0.5f,
                _settings.UseSoftZone ? _settings.SoftZoneSize.y * 0.5f : 0f);
            float moveZ = offsetFromCamera.z; // no dead zone on depth - a 2D game leaves this axis unmasked anyway

            Vector3 desiredPosition = previousPose.Position + Vector3.Scale(new Vector3(moveX, moveY, moveZ), _settings.FollowAxisMask);

            float damping = context.ReduceMotion ? 0f : _settings.PositionDamping;
            Vector3 newPosition = damping <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(previousPose.Position, desiredPosition, ref _dampVelocity, damping, Mathf.Infinity, deltaTime);

            return new CameraPose(newPosition, previousPose.Rotation, previousPose.OrthographicSize, previousPose.FieldOfView);
        }
    }
}
