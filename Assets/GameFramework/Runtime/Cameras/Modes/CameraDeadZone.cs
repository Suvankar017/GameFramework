using UnityEngine;

namespace GameFramework.Cameras.Modes
{
    /// <summary>
    /// Pure "rubber band" dead-zone/soft-zone math behind <see cref="FollowCameraMode"/> - kept as a
    /// standalone static class specifically so the axis math is unit-testable in isolation from
    /// damping/offset/axis-mask concerns (mirrors <c>Presentation.CameraShakeState</c>'s "kept
    /// separate so it's testable" reasoning).
    /// </summary>
    internal static class CameraDeadZone
    {
        /// <summary>Returns how far the camera should move on one axis given how far the target has
        /// drifted from the camera's current center (<paramref name="offset"/>). Zero while
        /// <paramref name="offset"/> stays within the dead zone; beyond it (and, if
        /// <paramref name="softZoneHalf"/> &gt; 0, eased over that additional band), returns exactly
        /// enough movement to keep the target at the dead zone's edge - the target itself is never
        /// forced back to center.</summary>
        public static float ClampAxis(float offset, float deadZoneHalf, float softZoneHalf)
        {
            float magnitude = Mathf.Abs(offset);
            float sign = Mathf.Sign(offset);

            if (magnitude <= deadZoneHalf)
            {
                return 0f;
            }

            float excess = magnitude - deadZoneHalf;

            if (softZoneHalf <= 0f)
            {
                return sign * excess;
            }

            if (excess <= softZoneHalf)
            {
                // Ease the camera in gradually across the soft-zone band instead of matching the
                // target's full excess movement immediately.
                float t = excess / softZoneHalf;
                return sign * excess * t;
            }

            // Beyond the soft zone entirely: the camera must fully catch up, minus what the soft
            // zone itself already accounted for at its own boundary, to avoid a visible pop.
            return sign * excess;
        }
    }
}
