using GameFramework.Core.Extensions;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Built-in <see cref="ICameraTarget"/> adapter over a plain <see cref="Transform"/> - the
    /// common case ("follow this GameObject"). Deliberately does not derive <see cref="Velocity"/>
    /// from frame-to-frame position deltas: doing so correctly needs a per-frame sample call with no
    /// natural owner (missing one frame produces a distorted delta), so <see cref="HasVelocity"/> is
    /// simply false here - a game that needs real velocity (e.g. for a look-ahead camera) implements
    /// <see cref="ICameraTarget"/> directly over its own Rigidbody/controller instead (see
    /// CLAUDE.md's Phase 11 brief, section 9: "do not require every target to provide every
    /// property").
    /// </summary>
    public sealed class TransformCameraTarget : ICameraTarget
    {
        private readonly Transform _transform;

        public TransformCameraTarget(Transform transform)
        {
            _transform = transform;
        }

        /// <summary>The wrapped <see cref="UnityEngine.Transform"/> itself - an external camera
        /// backend integration that needs a real <see cref="UnityEngine.Transform"/> (e.g. a
        /// Cinemachine adapter assigning <c>CinemachineVirtualCamera.Follow</c>) reads this rather
        /// than requiring a second, competing target abstraction. Not part of <see cref="ICameraTarget"/>
        /// itself, which stays engine-detail-free by design.</summary>
        public Transform Transform => _transform;

        public bool IsValid => !_transform.IsNullOrDestroyed();

        public Vector3 Position => IsValid ? _transform.position : Vector3.zero;
        public Quaternion Rotation => IsValid ? _transform.rotation : Quaternion.identity;

        public bool HasVelocity => false;
        public Vector3 Velocity => Vector3.zero;
    }
}
