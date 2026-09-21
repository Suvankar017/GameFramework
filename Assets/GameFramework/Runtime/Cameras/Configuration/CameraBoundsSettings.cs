using System;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>Authored world-space camera bounds (CLAUDE.md's Phase 11 brief, section 13).
    /// Exact for an orthographic camera (accounts for its half-extents); for a perspective camera
    /// this clamps only the camera's center position, with no extent compensation - the "reliable
    /// subset" the brief explicitly allows for, since a perspective camera's view extent depends on
    /// distance-to-subject, which this framework does not assume anything about.</summary>
    [Serializable]
    public sealed class CameraBoundsSettings
    {
        public bool Enabled;

        public float MinX = -10f;
        public float MaxX = 10f;
        public float MinY = -10f;
        public float MaxY = 10f;
    }
}
