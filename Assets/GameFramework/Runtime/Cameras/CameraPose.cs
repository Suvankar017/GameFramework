using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// The "Camera State" layer of the framework's pipeline (Target/Intent -> Camera Framework ->
    /// <b>Camera State</b> -> Camera Driver -> Unity Camera) - a pure, immutable snapshot of where a
    /// camera should be, computed entirely without touching <see cref="UnityEngine.Camera"/> or a
    /// <see cref="Transform"/>, so every mode/constraint/transition that produces one is unit
    /// testable in EditMode with no live camera.
    ///
    /// <see cref="OrthographicSize"/>/<see cref="FieldOfView"/> are carried together rather than as
    /// one polymorphic "zoom" value so a pose is meaningful regardless of the actual
    /// <see cref="Camera.orthographic"/> projection - only the one matching the camera's current
    /// projection mode is ever applied (see <see cref="CameraDriver"/>).
    /// </summary>
    public readonly struct CameraPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float OrthographicSize;
        public readonly float FieldOfView;

        public CameraPose(Vector3 position, Quaternion rotation, float orthographicSize, float fieldOfView)
        {
            Position = position;
            Rotation = rotation;
            OrthographicSize = orthographicSize;
            FieldOfView = fieldOfView;
        }

        /// <summary>Component-wise interpolation used by <see cref="CameraTransitionRunner"/> -
        /// <paramref name="t"/> is clamped to [0, 1], so a transition can never overshoot its
        /// target pose regardless of an authored easing curve's own range.</summary>
        public static CameraPose Lerp(CameraPose a, CameraPose b, float t)
        {
            return new CameraPose(
                Vector3.Lerp(a.Position, b.Position, t),
                Quaternion.Slerp(a.Rotation, b.Rotation, t),
                Mathf.Lerp(a.OrthographicSize, b.OrthographicSize, t),
                Mathf.Lerp(a.FieldOfView, b.FieldOfView, t));
        }
    }
}
