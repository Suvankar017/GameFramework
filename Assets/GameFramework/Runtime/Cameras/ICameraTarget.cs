using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Lightweight camera-target abstraction - gameplay hands a camera an
    /// <see cref="ICameraTarget"/>, never a concrete player/enemy/vehicle type, so no
    /// <see cref="GameFramework.Cameras"/> type ever needs to know a game's own gameplay types (see
    /// CLAUDE.md's Phase 11 brief, section 9). <see cref="TransformCameraTarget"/> is the built-in
    /// adapter over a plain <see cref="Transform"/> - a game can implement this directly (e.g. over
    /// a Rigidbody) when it needs <see cref="Velocity"/> for real.
    /// </summary>
    public interface ICameraTarget
    {
        /// <summary>False once the underlying object has been destroyed - a mode must treat this
        /// the same as "no target" (see CLAUDE.md's Phase 11 brief, section 35's "missing target"
        /// fallback) rather than reading stale <see cref="Position"/>/<see cref="Rotation"/>.</summary>
        bool IsValid { get; }

        Vector3 Position { get; }
        Quaternion Rotation { get; }

        /// <summary>False for a target that does not track velocity (e.g. <see cref="TransformCameraTarget"/>,
        /// which deliberately does not derive one - see its remarks). A mode must not assume
        /// <see cref="Velocity"/> is meaningful unless this is true.</summary>
        bool HasVelocity { get; }
        Vector3 Velocity { get; }
    }
}
