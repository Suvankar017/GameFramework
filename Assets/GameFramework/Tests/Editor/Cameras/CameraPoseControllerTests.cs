using GameFramework.Cameras.Configuration;
using GameFramework.Cameras.Modes;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class CameraPoseControllerTests
    {
        [Test]
        public void Tick_ComposesModeAndZoom_BothApplied()
        {
            var follow = new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 0f };
            var zoomSettings = new CameraZoomSettings { MinOrthographicSize = 1f, MaxOrthographicSize = 20f, ZoomDamping = 0f };
            var controller = new CameraPoseController();
            var initial = new CameraPose(Vector3.zero, Quaternion.identity, 5f, 60f);
            controller.Initialize(new FollowCameraMode(follow), zoomSettings, new CameraBoundsSettings(), orthographic: true, aspect: 1f, initial);

            var target = new FakeCameraTarget { Position = new Vector3(3f, 0f, 0f) };
            controller.SetTarget(target);
            controller.SetZoom(8f);

            CameraPose result = controller.Tick(0.1f);

            Assert.AreEqual(3f, result.Position.x, 0.0001f);
            Assert.AreEqual(8f, result.OrthographicSize, 0.0001f);
        }

        [Test]
        public void Tick_AppliesBoundsAfterMode()
        {
            var follow = new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 0f };
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -2f, MaxX = 2f, MinY = -2f, MaxY = 2f };
            var controller = new CameraPoseController();
            var initial = new CameraPose(Vector3.zero, Quaternion.identity, 0f, 60f); // orthographicSize 0 => no half-extent
            controller.Initialize(new FollowCameraMode(follow), new CameraZoomSettings { ZoomDamping = 0f }, bounds, orthographic: true, aspect: 1f, initial);

            var target = new FakeCameraTarget { Position = new Vector3(100f, 0f, 0f) };
            controller.SetTarget(target);

            CameraPose result = controller.Tick(0.1f);

            Assert.LessOrEqual(result.Position.x, 2.0001f);
        }

        [Test]
        public void Tick_TargetDestroyedMidFollow_HoldsLastPoseInstead_OfThrowingOrJumping()
        {
            var follow = new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 0f };
            var controller = new CameraPoseController();
            var initial = new CameraPose(Vector3.zero, Quaternion.identity, 5f, 60f);
            controller.Initialize(new FollowCameraMode(follow), new CameraZoomSettings { ZoomDamping = 0f }, new CameraBoundsSettings(), orthographic: true, aspect: 1f, initial);

            var target = new FakeCameraTarget { Position = new Vector3(5f, 0f, 0f) };
            controller.SetTarget(target);
            controller.Tick(0.1f); // now at x=5

            target.IsValid = false; // simulate destruction
            CameraPose result = controller.Tick(0.1f);

            Assert.AreEqual(5f, result.Position.x, 0.0001f); // held, not reset to zero or thrown
        }

        [Test]
        public void Snap_ConvergesPositionAndZoomInstantly_IgnoringConfiguredDamping()
        {
            var follow = new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 5f }; // heavy damping configured
            var controller = new CameraPoseController();
            var initial = new CameraPose(Vector3.zero, Quaternion.identity, 5f, 60f);
            controller.Initialize(new FollowCameraMode(follow), new CameraZoomSettings { ZoomDamping = 5f }, new CameraBoundsSettings(), orthographic: true, aspect: 1f, initial);
            var target = new FakeCameraTarget { Position = new Vector3(9f, 9f, 9f) };
            controller.SetTarget(target);
            controller.SetZoom(10f);
            controller.Tick(0.02f); // one normal, heavily-damped tick - barely moves

            controller.Snap();

            Assert.AreEqual(9f, controller.CurrentPose.Position.x, 0.0001f);
            Assert.AreEqual(10f, controller.CurrentPose.OrthographicSize, 0.0001f);
        }

        [Test]
        public void Snap_StaticMode_HoldsCurrentPose_NeverJumpsToTarget()
        {
            var controller = new CameraPoseController();
            var initial = new CameraPose(new Vector3(1f, 2f, -10f), Quaternion.identity, 5f, 60f);
            controller.Initialize(new StaticCameraMode(), new CameraZoomSettings { ZoomDamping = 0f }, new CameraBoundsSettings(), orthographic: true, aspect: 1f, initial);
            var target = new FakeCameraTarget { Position = new Vector3(99f, 99f, 99f) };
            controller.SetTarget(target);

            controller.Snap();

            Assert.AreEqual(new Vector3(1f, 2f, -10f), controller.CurrentPose.Position);
        }
    }
}
