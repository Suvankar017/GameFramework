using GameFramework.Cameras.Configuration;
using GameFramework.Cameras.Modes;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class FollowCameraModeTests
    {
        private static CameraPose Pose(Vector3 position) => new CameraPose(position, Quaternion.identity, 5f, 60f);

        [Test]
        public void ComputePose_NoTarget_ReturnsPreviousPoseUnchanged()
        {
            var mode = new FollowCameraMode(new CameraFollowSettings { PositionDamping = 0f });
            CameraPose previous = Pose(new Vector3(1f, 2f, 3f));

            CameraPose result = mode.ComputePose(new CameraModeContext(null), previous, 0.1f);

            Assert.AreEqual(previous.Position, result.Position);
        }

        [Test]
        public void ComputePose_InvalidTarget_ReturnsPreviousPoseUnchanged()
        {
            var mode = new FollowCameraMode(new CameraFollowSettings { PositionDamping = 0f });
            var target = new FakeCameraTarget { IsValid = false, Position = new Vector3(100f, 0f, 0f) };
            CameraPose previous = Pose(Vector3.zero);

            CameraPose result = mode.ComputePose(new CameraModeContext(target), previous, 0.1f);

            Assert.AreEqual(Vector3.zero, result.Position);
        }

        [Test]
        public void ComputePose_NoDamping_NoDeadZone_SnapsDirectlyToTargetPlusOffset()
        {
            var settings = new CameraFollowSettings
            {
                Offset = new Vector3(0f, 1f, -5f),
                FollowAxisMask = Vector3.one,
                PositionDamping = 0f
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(10f, 0f, 0f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), Pose(Vector3.zero), 0.1f);

            Assert.AreEqual(new Vector3(10f, 1f, -5f), result.Position);
        }

        [Test]
        public void ComputePose_FixedAxis_NeverMoves()
        {
            var settings = new CameraFollowSettings
            {
                FollowAxisMask = new Vector3(1f, 0f, 0f), // Y and Z fixed
                PositionDamping = 0f
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(10f, 99f, 99f) };
            CameraPose previous = Pose(new Vector3(0f, 3f, -8f));

            CameraPose result = mode.ComputePose(new CameraModeContext(target), previous, 0.1f);

            Assert.AreEqual(10f, result.Position.x, 0.0001f);
            Assert.AreEqual(3f, result.Position.y, 0.0001f); // untouched
            Assert.AreEqual(-8f, result.Position.z, 0.0001f); // untouched
        }

        [Test]
        public void ComputePose_TargetInsideDeadZone_CameraDoesNotMove()
        {
            var settings = new CameraFollowSettings
            {
                Offset = Vector3.zero,
                FollowAxisMask = Vector3.one,
                PositionDamping = 0f,
                DeadZoneSize = new Vector2(4f, 4f) // half = 2 on each axis
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(1.5f, -1.5f, 0f) }; // within +-2 half-zone
            CameraPose previous = Pose(Vector3.zero);

            CameraPose result = mode.ComputePose(new CameraModeContext(target), previous, 0.1f);

            Assert.AreEqual(Vector3.zero, result.Position);
        }

        [Test]
        public void ComputePose_TargetExitsDeadZone_CameraMovesMinimumAmountToRestoreEdge()
        {
            var settings = new CameraFollowSettings
            {
                Offset = Vector3.zero,
                FollowAxisMask = Vector3.one,
                PositionDamping = 0f,
                DeadZoneSize = new Vector2(4f, 4f) // half = 2
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(5f, 0f, 0f) }; // 3 beyond the half-zone edge
            CameraPose previous = Pose(Vector3.zero);

            CameraPose result = mode.ComputePose(new CameraModeContext(target), previous, 0.1f);

            Assert.AreEqual(3f, result.Position.x, 0.0001f); // moved exactly the excess, not all the way to the target
            Assert.AreEqual(0f, result.Position.y, 0.0001f);
        }

        [Test]
        public void ComputePose_ReduceMotion_SkipsDampingEvenWhenConfigured()
        {
            var settings = new CameraFollowSettings
            {
                FollowAxisMask = Vector3.one,
                PositionDamping = 5f // heavy damping configured
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(10f, 0f, 0f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target, reduceMotion: true), Pose(Vector3.zero), 0.02f);

            Assert.AreEqual(10f, result.Position.x, 0.0001f); // reached instantly despite damping configured
        }

        [Test]
        public void ComputePose_WithDamping_MovesPartiallyTowardTarget_NotAllTheWay()
        {
            var settings = new CameraFollowSettings
            {
                FollowAxisMask = Vector3.one,
                PositionDamping = 1f
            };
            var mode = new FollowCameraMode(settings);
            var target = new FakeCameraTarget { Position = new Vector3(10f, 0f, 0f) };

            CameraPose result = mode.ComputePose(new CameraModeContext(target), Pose(Vector3.zero), 0.02f);

            Assert.Greater(result.Position.x, 0f);
            Assert.Less(result.Position.x, 10f);
        }
    }
}
