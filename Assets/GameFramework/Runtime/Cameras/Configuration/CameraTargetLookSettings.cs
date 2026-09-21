using System;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>Authored <see cref="Modes.TargetLookCameraMode"/> parameters - camera position
    /// tracks the target at a fixed offset while rotation stays aimed at it (CLAUDE.md's Phase 11
    /// brief, section 8: "track target position while maintaining configurable camera
    /// positioning").</summary>
    [Serializable]
    public sealed class CameraTargetLookSettings
    {
        public Vector3 Offset = new Vector3(0f, 2f, -6f);

        [Min(0f)] public float PositionDamping = 0.2f;

        [Tooltip("Seconds for rotation to settle toward the target. 0 = snap directly.")]
        [Min(0f)] public float RotationDamping = 0.15f;
    }
}
