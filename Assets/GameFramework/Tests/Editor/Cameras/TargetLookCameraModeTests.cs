using GameFramework.Cameras.Configuration;
using GameFramework.Cameras.Modes;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class TargetLookCameraModeTests
    {
        private static CameraPose Pose(Vector3 position) => new CameraPose(position, Quaternion.identity, 5f, 60f);

        [Test]
        public void ComputePose_NoTarget_ReturnsPreviousPoseUnchanged()
        {
            var mode = new TargetLookCameraMode(new CameraTargetLookSettings());
            CameraPose previous = Pose(new Vector3(1f, 2f, 3f));

            CameraPose result = mode.ComputePose(new CameraModeContext(null), previous, 0.1f);

            Assert.AreEqual(previous.Position, result.Position);
        }

        [Test]
        public void ComputePose_NoDamping_PositionMatchesTargetPlusOffset()
        {
            var settings = new CameraTargetLookSettings { Offset = new Vector3(0f, 2f, -6f), PositionDamping = 0f, RotationDamping = 0f };
            var mode = new TargetLookCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(5f, 0f, 0f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), Pose(Vector3.zero), 0.1f);

            Assert.AreEqual(new Vector3(5f, 2f, -6f), result.Position);
        }

        [Test]
        public void ComputePose_NoDamping_RotationLooksAtTarget()
        {
            var settings = new CameraTargetLookSettings { Offset = new Vector3(0f, 0f, -10f), PositionDamping = 0f, RotationDamping = 0f };
            var mode = new TargetLookCameraMode(settings);
            var target = new FakeCameraTarget { Position = Vector3.zero };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), Pose(new Vector3(0f, 0f, -10f)), 0.1f);

            Vector3 forward = result.Rotation * Vector3.forward;
            Assert.Greater(Vector3.Dot(forward, Vector3.forward), 0.99f); // looking toward +Z, at the target
        }

        [Test]
        public void ComputePose_WithDamping_PositionMovesPartiallyTowardTarget()
        {
            var settings = new CameraTargetLookSettings { Offset = Vector3.zero, PositionDamping = 1f };
            var mode = new TargetLookCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(10f, 0f, 0f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), Pose(Vector3.zero), 0.02f);

            Assert.Greater(result.Position.x, 0f);
            Assert.Less(result.Position.x, 10f);
        }
    }
}
