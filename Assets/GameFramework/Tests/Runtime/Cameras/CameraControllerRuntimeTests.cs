using System.Reflection;
using GameFramework.Cameras.Configuration;
using GameFramework.Cameras.Modes;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Cameras.Tests
{
    /// <summary>Covers <see cref="CameraController"/> behavior that needs a real GameObject
    /// lifecycle (<c>Awake</c>/<c>OnEnable</c>/<c>OnDisable</c> firing from actual
    /// <see cref="GameObject.SetActive"/> transitions) - reflection-injects
    /// <c>ICameraService</c> directly, bypassing <see cref="Runtime.Bootstrap.GameBootstrapper"/>,
    /// the same technique <see cref="CameraDriverRuntimeTests"/> uses.</summary>
    public class CameraControllerRuntimeTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private CameraService _service;
        private GameObject _go;
        private CameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _registry = new ServiceRegistry();
            _events = new EventService();
            _registry.Register<IEventService>(_events);
            _events.Initialize(_registry);
            _registry.MarkInitialized(typeof(IEventService));

            _service = new CameraService();
            _service.Initialize(_registry);

            _go = new GameObject("Controller");
            _go.transform.position = new Vector3(2f, 3f, -5f);
            _controller = _go.AddComponent<CameraController>();
            InjectService(_controller, _service);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
            if (_go != null)
            {
                Object.Destroy(_go);
            }
        }

        private static void InjectService(CameraController controller, ICameraService service)
        {
            typeof(CameraController).GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, service);
        }

        [Test]
        public void Awake_NoTarget_InitialPoseMatchesTransformPosition()
        {
            Assert.AreEqual(_go.transform.position, _controller.CurrentPose.Position);
        }

        [Test]
        public void SetTargetTransform_ThenComputePose_FollowsIt()
        {
            // Default (no CameraConfiguration assigned) mode is Static, which deliberately never
            // moves - see StaticCameraMode's remarks - so this test explicitly opts into Follow.
            _controller.SetMode(new FollowCameraMode(new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 0f }));
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(20f, 0f, 0f);

            _controller.SetTargetTransform(targetGo.transform);
            CameraPose pose = _controller.ComputePose(1f);

            Assert.Greater(pose.Position.x, 0f);

            Object.Destroy(targetGo);
        }

        [Test]
        public void ClearTarget_HasTargetBecomesFalse()
        {
            var targetGo = new GameObject("Target");
            _controller.SetTargetTransform(targetGo.transform);
            Assert.IsTrue(_controller.HasTarget);

            _controller.ClearTarget();

            Assert.IsFalse(_controller.HasTarget);
            Object.Destroy(targetGo);
        }

        [Test]
        public void Snap_ResetsPoseToCurrentTargetImmediately()
        {
            // Default (no CameraConfiguration assigned) mode is Static, which never moves even on
            // Snap - see StaticCameraMode's remarks - so this test explicitly opts into Follow.
            _controller.SetMode(new FollowCameraMode(new CameraFollowSettings { FollowAxisMask = Vector3.one, PositionDamping = 0.3f }));
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(15f, 0f, 0f);
            _controller.SetTargetTransform(targetGo.transform);

            _controller.Snap();

            Assert.AreEqual(15f, _controller.CurrentPose.Position.x, 0.001f);
            Object.Destroy(targetGo);
        }
    }
}
