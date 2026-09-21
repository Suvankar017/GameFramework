using System;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>
    /// Authored blend used by <see cref="CameraTransitionRunner"/> whenever <see cref="CameraDriver"/>
    /// switches which <see cref="CameraController"/> is active (activation, override push/pop) -
    /// one combined position+rotation+zoom blend rather than three independently-timed ones
    /// (CLAUDE.md's Phase 11 brief, section 19 lists "Combined Transition" as one of the supported
    /// kinds; separate per-channel durations were not built since no concrete need for them was
    /// identified - see Framework.md's Known Limitations for this intentional scope reduction).
    /// </summary>
    [Serializable]
    public sealed class CameraTransitionSettings
    {
        [Tooltip("0 = immediate (no blend); switch snaps straight to the newly active camera's pose.")]
        [Min(0f)] public float Duration;

        [Tooltip("Evaluated over [0, 1] normalized elapsed time. Left empty, a linear blend is used.")]
        public AnimationCurve Ease;
    }
}
