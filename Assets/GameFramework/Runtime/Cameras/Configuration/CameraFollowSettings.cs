using System;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>Authored <see cref="Modes.FollowCameraMode"/> parameters - offset, per-axis follow
    /// mask (CLAUDE.md's Phase 11 brief, section 10: "X -&gt; follow, Y -&gt; follow, Z -&gt; fixed"),
    /// damping, and an optional dead zone/soft zone (sections 11-12).</summary>
    [Serializable]
    public sealed class CameraFollowSettings
    {
        public Vector3 Offset = new Vector3(0f, 0f, -10f);

        [Tooltip("1 = this axis follows the target; 0 = this axis stays exactly where it was when " +
            "this controller was last snapped, ignoring the target entirely.")]
        public Vector3 FollowAxisMask = new Vector3(1f, 1f, 0f);

        [Tooltip("SmoothDamp time in seconds. 0 = no damping, snap directly to the follow target.")]
        [Min(0f)] public float PositionDamping = 0.2f;

        [Tooltip("Full width/height (world units) of the zone the target can move within before " +
            "the camera starts moving at all.")]
        public Vector2 DeadZoneSize = Vector2.zero;

        [Tooltip("When enabled, the camera eases in gradually over an additional band beyond the " +
            "dead zone instead of immediately matching the target's full excess movement.")]
        public bool UseSoftZone;

        [Tooltip("Extra width/height (world units), beyond DeadZoneSize, over which the soft-zone " +
            "ease applies.")]
        public Vector2 SoftZoneSize = Vector2.zero;
    }
}
