using GameFramework.Cameras.Modes;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class StaticCameraModeTests
    {
        [Test]
        public void ComputePose_AlwaysReturnsPreviousPoseUnchanged_EvenWithATarget()
        {
            var mode = new StaticCameraMode();
            var previous = new CameraPose(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 45f, 0f), 5f, 60f);
            var target = new FakeCameraTarget { Position = new Vector3(99f, 99f, 99f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), previous, 0.5f);

            Assert.AreEqual(previous.Position, result.Position);
            Assert.AreEqual(previous.Rotation, result.Rotation);
        }
    }
}
