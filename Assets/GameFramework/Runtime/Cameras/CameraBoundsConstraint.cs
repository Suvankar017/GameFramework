using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Pure world-space camera bounds clamp (CLAUDE.md's Phase 11 brief, section 13). Exact for an
    /// orthographic camera (subtracts its half-extents so the *view*, not just the center point,
    /// never leaves the bounds); for a perspective camera this clamps only the center position, with
    /// no extent compensation - see <see cref="CameraBoundsSettings"/>'s remarks for why.
    /// </summary>
    internal static class CameraBoundsConstraint
    {
        public static Vector3 Clamp(Vector3 position, CameraBoundsSettings bounds, bool orthographic, float orthographicSize, float aspect)
        {
            if (bounds == null || !bounds.Enabled)
            {
                return position;
            }

            float halfWidth = 0f;
            float halfHeight = 0f;

            if (orthographic)
            {
                halfHeight = orthographicSize;
                halfWidth = orthographicSize * aspect;
            }

            float x = ClampAxis(position.x, bounds.MinX, bounds.MaxX, halfWidth);
            float y = ClampAxis(position.y, bounds.MinY, bounds.MaxY, halfHeight);

            return new Vector3(x, y, position.z);
        }

        private static float ClampAxis(float value, float min, float max, float half)
        {
            float adjustedMin = min + half;
            float adjustedMax = max - half;

            // Bounds narrower than the camera's own view on this axis: center within the authored
            // bounds instead of an inverted/degenerate clamp range (CLAUDE.md's Phase 11 brief,
            // section 13's "edge cases").
            return adjustedMin <= adjustedMax
                ? Mathf.Clamp(value, adjustedMin, adjustedMax)
                : (min + max) * 0.5f;
        }
    }
}
