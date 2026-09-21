using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    internal sealed class FakeCameraTarget : ICameraTarget
    {
        public bool IsValid { get; set; } = true;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public bool HasVelocity { get; set; }
        public Vector3 Velocity { get; set; }
    }
}
