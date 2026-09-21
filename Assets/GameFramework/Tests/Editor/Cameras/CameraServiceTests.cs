using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Cameras.Tests
{
    public class CameraServiceTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private SettingsService _settings;
        private CameraService _service;
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _events, out _settings);
            _service = new CameraService();
            _service.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
            foreach (GameObject go in _createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdObjects.Clear();
        }

        private CameraController CreateController(string name = "Camera")
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go.AddComponent<CameraController>();
        }

        [Test]
        public void RegisterCamera_ReturnsValidId()
        {
            CameraId id = _service.RegisterCamera(CreateController(), priority: 1, owner: "Test");

            Assert.IsTrue(id.IsValid);
            Assert.IsTrue(_service.IsRegistered(id));
        }

        [Test]
        public void RegisterCamera_SameInstanceTwice_ReturnsSameId_DoesNotDuplicate()
        {
            CameraController controller = CreateController();

            CameraId first = _service.RegisterCamera(controller);
            CameraId second = _service.RegisterCamera(controller);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void RegisterCamera_Null_ReturnsNoneId()
        {
            CameraId id = _service.RegisterCamera(null);
            Assert.IsFalse(id.IsValid);
        }

        [Test]
        public void RegisterCamera_PublishesCameraRegisteredEvent()
        {
            CameraRegisteredEvent? received = null;
            _events.Subscribe<CameraRegisteredEvent>(e => received = e);

            CameraId id = _service.RegisterCamera(CreateController());

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(id, received.Value.Id);
        }

        [Test]
        public void UnregisterCamera_UnknownId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.UnregisterCamera(CameraId.New()));
        }

        [Test]
        public void UnregisterCamera_RemovesRegistration()
        {
            CameraId id = _service.RegisterCamera(CreateController());

            _service.UnregisterCamera(id);

            Assert.IsFalse(_service.IsRegistered(id));
        }

        [Test]
        public void Activate_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_service.Activate(CameraId.New()));
        }

        [Test]
        public void Activate_KnownId_BecomesActiveCamera()
        {
            CameraId id = _service.RegisterCamera(CreateController());

            bool result = _service.Activate(id);

            Assert.IsTrue(result);
            Assert.AreEqual(id, _service.ActiveCameraId);
        }

        [Test]
        public void Activate_PublishesActiveCameraChangedEvent()
        {
            CameraId id = _service.RegisterCamera(CreateController());
            ActiveCameraChangedEvent? received = null;
            _events.Subscribe<ActiveCameraChangedEvent>(e => received = e);

            _service.Activate(id);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(id, received.Value.Current);
        }

        [Test]
        public void Activate_SameIdTwice_DoesNotRefireChangedEvent()
        {
            CameraId id = _service.RegisterCamera(CreateController());
            _service.Activate(id);
            int fireCount = 0;
            _service.ActiveCameraChanged += (previous, current) => fireCount++;

            _service.Activate(id);

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void UnregisterCamera_ActiveCamera_ClearsActiveCameraId()
        {
            CameraId id = _service.RegisterCamera(CreateController());
            _service.Activate(id);

            _service.UnregisterCamera(id);

            Assert.IsFalse(_service.ActiveCameraId.IsValid);
        }

        [Test]
        public void PushOverride_BecomesActiveImmediately_RegardlessOfBaseActivation()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);

            _service.PushOverride(overrideId);

            Assert.AreEqual(overrideId, _service.ActiveCameraId);
        }

        [Test]
        public void PopOverride_RestoresBaseCamera()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);
            ICameraOverrideHandle handle = _service.PushOverride(overrideId);

            handle.Release();

            Assert.AreEqual(baseId, _service.ActiveCameraId);
        }

        [Test]
        public void NestedOverrides_PoppedInOrder_RestoreCorrectly()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId eventId = _service.RegisterCamera(CreateController("Event"));
            CameraId pauseId = _service.RegisterCamera(CreateController("Pause"));
            _service.Activate(baseId);

            ICameraOverrideHandle eventHandle = _service.PushOverride(eventId);
            ICameraOverrideHandle pauseHandle = _service.PushOverride(pauseId);
            Assert.AreEqual(pauseId, _service.ActiveCameraId);

            pauseHandle.Release();
            Assert.AreEqual(eventId, _service.ActiveCameraId, "Popping the top override should restore the one beneath it.");

            eventHandle.Release();
            Assert.AreEqual(baseId, _service.ActiveCameraId, "Popping the last override should restore the base camera.");
        }

        [Test]
        public void PopOverride_OutOfOrder_RemovesFromWhereverItSits_WithoutCorruptingStack()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId eventId = _service.RegisterCamera(CreateController("Event"));
            CameraId pauseId = _service.RegisterCamera(CreateController("Pause"));
            _service.Activate(baseId);

            ICameraOverrideHandle eventHandle = _service.PushOverride(eventId);
            ICameraOverrideHandle pauseHandle = _service.PushOverride(pauseId);

            eventHandle.Release(); // released while NOT on top
            Assert.AreEqual(pauseId, _service.ActiveCameraId, "Removing a non-top override must not disturb the current top.");

            pauseHandle.Release();
            Assert.AreEqual(baseId, _service.ActiveCameraId);
        }

        [Test]
        public void ReleaseHandle_Twice_IsSafe_DoesNotDoubleRestore()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);
            ICameraOverrideHandle handle = _service.PushOverride(overrideId);

            handle.Release();
            Assert.DoesNotThrow(() => handle.Release());
            Assert.AreEqual(baseId, _service.ActiveCameraId);
        }

        [Test]
        public void UnregisterCamera_WhileOverride_RemovesFromStack_AndRestoresBase()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);
            _service.PushOverride(overrideId);

            _service.UnregisterCamera(overrideId);

            Assert.AreEqual(baseId, _service.ActiveCameraId);
        }

        [Test]
        public void SetTarget_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_service.SetTarget(CameraId.New(), new FakeCameraTarget()));
        }

        [Test]
        public void SetTarget_KnownId_UpdatesControllerHasTarget()
        {
            CameraController controller = CreateController();
            CameraId id = _service.RegisterCamera(controller);

            bool result = _service.SetTarget(id, new FakeCameraTarget { IsValid = true });

            Assert.IsTrue(result);
            Assert.IsTrue(controller.HasTarget);
        }

        [Test]
        public void ClearTarget_RemovesTarget()
        {
            CameraController controller = CreateController();
            CameraId id = _service.RegisterCamera(controller);
            _service.SetTarget(id, new FakeCameraTarget { IsValid = true });

            _service.ClearTarget(id);

            Assert.IsFalse(controller.HasTarget);
        }

        [Test]
        public void ResetCamera_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_service.ResetCamera(CameraId.New()));
        }

        [Test]
        public void GetState_UnknownId_ReturnsInvalidId()
        {
            CameraRuntimeState state = _service.GetState(CameraId.New());
            Assert.IsFalse(state.Id.IsValid);
        }

        [Test]
        public void GetState_ActiveOverride_ReportsIsActiveAndIsOverride()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);
            _service.PushOverride(overrideId);

            CameraRuntimeState state = _service.GetState(overrideId);

            Assert.IsTrue(state.IsActive);
            Assert.IsTrue(state.IsOverride);
        }

        [Test]
        public void GetState_InactiveBase_ReportsNotActive()
        {
            CameraId baseId = _service.RegisterCamera(CreateController("Base"));
            CameraId overrideId = _service.RegisterCamera(CreateController("Override"));
            _service.Activate(baseId);
            _service.PushOverride(overrideId);

            CameraRuntimeState state = _service.GetState(baseId);

            Assert.IsFalse(state.IsActive);
            Assert.IsFalse(state.IsOverride);
        }
    }
}
