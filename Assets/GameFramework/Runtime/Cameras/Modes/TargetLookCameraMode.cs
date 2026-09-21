using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras.Modes
{
    /// <summary>Camera position tracks the target at a fixed offset (damped) while rotation stays
    /// aimed at the target (separately damped) - CLAUDE.md's Phase 11 brief, section 8. A missing or
    /// destroyed target holds the previous pose unchanged (section 35).</summary>
    internal sealed class TargetLookCameraMode : ICameraMode
    {
        private readonly CameraTargetLookSettings _settings;
        private Vector3 _dampVelocity;

        public TargetLookCameraMode(CameraTargetLookSettings settings)
        {
            _settings = settings;
        }

        public CameraPose ComputePose(CameraModeContext context, CameraPose previousPose, float deltaTime)
        {
            if (!context.HasTarget)
            {
                return previousPose;
            }

            float positionDamping = context.ReduceMotion ? 0f : _settings.PositionDamping;
            Vector3 desiredPosition = context.Target.Position + _settings.Offset;
            Vector3 newPosition = positionDamping <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(previousPose.Position, desiredPosition, ref _dampVelocity, positionDamping, Mathf.Infinity, deltaTime);

            Vector3 lookDirection = context.Target.Position - newPosition;
            Quaternion desiredRotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection, Vector3.up)
                : previousPose.Rotation;

            float rotationDamping = context.ReduceMotion ? 0f : _settings.RotationDamping;
            Quaternion newRotation = rotationDamping <= 0f
                ? desiredRotation
                : Quaternion.Slerp(previousPose.Rotation, desiredRotation, 1f - Mathf.Exp(-deltaTime / Mathf.Max(0.0001f, rotationDamping)));

            return new CameraPose(newPosition, newRotation, previousPose.OrthographicSize, previousPose.FieldOfView);
        }
    }
}
