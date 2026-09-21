using System;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>Authored zoom parameters - covers both projection modes (CLAUDE.md's Phase 11
    /// brief, sections 14-15); only the pair matching the actual <see cref="UnityEngine.Camera.orthographic"/>
    /// value is ever applied by a given <see cref="CameraController"/>.</summary>
    [Serializable]
    public sealed class CameraZoomSettings
    {
        [Min(0.01f)] public float MinOrthographicSize = 2f;
        [Min(0.01f)] public float MaxOrthographicSize = 15f;
        [Min(0.01f)] public float DefaultOrthographicSize = 5f;

        [Range(1f, 179f)] public float MinFieldOfView = 20f;
        [Range(1f, 179f)] public float MaxFieldOfView = 90f;
        [Range(1f, 179f)] public float DefaultFieldOfView = 60f;

        [Tooltip("SmoothDamp time in seconds. 0 = no damping, apply zoom changes instantly.")]
        [Min(0f)] public float ZoomDamping = 0.15f;
    }
}
