using GameFramework.Cameras.Modes;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class ManualCameraModeTests
    {
        [Test]
        public void ComputePose_BeforeAnyPoseSupplied_ReturnsPreviousPoseUnchanged()
        {
            var mode = new ManualCameraMode();
            var previous = new CameraPose(new Vector3(1f, 2f, 3f), Quaternion.identity, 5f, 60f);

            CameraPose result = mode.ComputePose(new CameraModeContext(null), previous, 0.1f);

            Assert.AreEqual(previous.Position, result.Position);
        }

        [Test]
        public void ComputePose_AfterSetPose_ReturnsSuppliedPose_RegardlessOfPreviousPose()
        {
            var mode = new ManualCameraMode();
            var supplied = new CameraPose(new Vector3(7f, 8f, 9f), Quaternion.identity, 3f, 50f);
            mode.SetPose(supplied);

            CameraPose result = mode.ComputePose(new CameraModeContext(null), new CameraPose(Vector3.zero, Quaternion.identity, 1f, 1f), 0.1f);

            Assert.AreEqual(supplied.Position, result.Position);
            Assert.AreEqual(supplied.OrthographicSize, result.OrthographicSize);
        }
    }
}
