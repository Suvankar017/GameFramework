using System.Collections;
using System.Reflection;
using Cinemachine;
using GameFramework.Cameras.Configuration;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameFramework.Cameras.CinemachineIntegration.Tests
{
    /// <summary>
    /// Covers the real Cinemachine wiring this integration adds - activation mapping to
    /// <c>CinemachineVirtualCamera.Priority</c>, target mirroring onto Follow/LookAt, damped zoom
    /// writing to the vcam's lens, and confiner bounds generation - over real engine frames (Play
    /// Mode, mirroring <c>Cameras.Tests.CameraDriverRuntimeTests</c>'s own reasoning for why this
    /// needs a running engine rather than EditMode).
    ///
    /// Bypasses <see cref="Runtime.Bootstrap.GameBootstrapper"/> entirely by reflection-injecting
    /// <see cref="CinemachineCameraBackend"/>'s private <c>_service</c>/<c>_events</c> fields directly
    /// (the same technique <c>CameraDriverRuntimeTests.InjectService</c> already uses), and by
    /// reflection-invoking its private <c>OnActiveCameraChanged</c> handler directly instead of
    /// wiring the real <see cref="ICameraService.ActiveCameraChanged"/> event subscription - this
    /// tests the same logic the real subscription would run, without needing to reason about C#
    /// event/delegate lifetime inside a test fixture. <c>LateUpdate</c> itself is never invoked via
    /// reflection - it is exercised for real via <c>yield return null</c>, same as
    /// <c>CameraDriverRuntimeTests</c>.
    /// </summary>
    public class CinemachineCameraAdapterRuntimeTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private CameraService _service;

        private GameObject _brainGameObject;
        private CinemachineCameraBackend _backend;

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

            _brainGameObject = new GameObject("Brain");
            _brainGameObject.AddComponent<Camera>();
            _brainGameObject.AddComponent<CinemachineBrain>();
            _backend = _brainGameObject.AddComponent<CinemachineCameraBackend>();

            SetField(_backend, "_service", _service);
            SetField(_backend, "_events", _events);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
            if (_brainGameObject != null)
            {
                Object.Destroy(_brainGameObject);
            }
        }

        private (CameraController controller, CinemachineVirtualCamera vcam, CinemachineCameraAdapter adapter, GameObject go) CreateAdapter(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false); // keep Awake/OnEnable from firing before the serialized fields below are set

            CameraController controller = go.AddComponent<CameraController>();
            CinemachineVirtualCamera vcam = go.AddComponent<CinemachineVirtualCamera>();
            CinemachineCameraAdapter adapter = go.AddComponent<CinemachineCameraAdapter>();

            SetField(adapter, "_controller", controller);
            SetField(adapter, "_virtualCamera", vcam);
            SetField(adapter, "_backend", _backend);

            go.SetActive(true);
            return (controller, vcam, adapter, go);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        private static void InvokeActiveCameraChanged(CinemachineCameraBackend backend, CameraId previous, CameraId current)
        {
            MethodInfo method = typeof(CinemachineCameraBackend).GetMethod("OnActiveCameraChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(backend, new object[] { previous, current });
        }

        [UnityTest]
        public IEnumerator Activation_BoostsPriority_AndDeactivationRestoresBase()
        {
            (CameraController controllerA, CinemachineVirtualCamera vcamA, CinemachineCameraAdapter adapterA, GameObject goA) = CreateAdapter("A");
            (CameraController controllerB, CinemachineVirtualCamera vcamB, CinemachineCameraAdapter adapterB, GameObject goB) = CreateAdapter("B");
            yield return null;

            _backend.Register(adapterA);
            _backend.Register(adapterB);

            CameraId idA = _service.RegisterCamera(controllerA);
            CameraId idB = _service.RegisterCamera(controllerB);

            InvokeActiveCameraChanged(_backend, CameraId.None, idA);
            Assert.AreEqual(adapterA.BasePriority + CinemachineCameraAdapter.ActivePriorityBoost, vcamA.Priority);
            Assert.AreEqual(adapterB.BasePriority, vcamB.Priority);

            InvokeActiveCameraChanged(_backend, idA, idB);
            Assert.AreEqual(adapterA.BasePriority, vcamA.Priority);
            Assert.AreEqual(adapterB.BasePriority + CinemachineCameraAdapter.ActivePriorityBoost, vcamB.Priority);

            Object.Destroy(goA);
            Object.Destroy(goB);
        }

        [UnityTest]
        public IEnumerator ActiveCameraChanged_MirrorsControllerTarget_OntoFollowAndLookAt()
        {
            (CameraController controller, CinemachineVirtualCamera vcam, CinemachineCameraAdapter adapter, GameObject go) = CreateAdapter("Target");
            yield return null;

            _backend.Register(adapter);
            CameraId id = _service.RegisterCamera(controller);

            var targetGo = new GameObject("PlayerTarget");
            controller.SetTargetTransform(targetGo.transform);

            InvokeActiveCameraChanged(_backend, CameraId.None, id);

            Assert.AreSame(targetGo.transform, vcam.Follow);
            Assert.AreSame(targetGo.transform, vcam.LookAt);

            Object.Destroy(go);
            Object.Destroy(targetGo);
        }

        [UnityTest]
        public IEnumerator LateUpdate_TicksActiveAdapterZoom_WritesOrthographicSize()
        {
            (CameraController controller, CinemachineVirtualCamera vcam, CinemachineCameraAdapter adapter, GameObject go) = CreateAdapter("Zoom");
            // LensSettings.Orthographic is derived (re-synced from the real Camera every frame) -
            // ModeOverride is Cinemachine's own documented way to force it deterministically for a test.
            vcam.m_Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vcam.m_Lens.OrthographicSize = 5f;
            yield return null;

            _backend.Register(adapter);
            CameraId id = _service.RegisterCamera(controller);
            InvokeActiveCameraChanged(_backend, CameraId.None, id);

            controller.SetZoom(9f);

            yield return null;
            yield return null; // allow damping to settle toward the requested zoom

            Assert.AreEqual(9f, vcam.m_Lens.OrthographicSize, 0.5f);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Start_GeneratesConfinerBounds_WhenBoundsEnabledAndNoShapeAuthored()
        {
            var go = new GameObject("Bounded");
            go.SetActive(false);

            CameraController controller = go.AddComponent<CameraController>();
            CinemachineVirtualCamera vcam = go.AddComponent<CinemachineVirtualCamera>();
            CinemachineConfiner2D confiner = go.AddComponent<CinemachineConfiner2D>();
            CinemachineCameraAdapter adapter = go.AddComponent<CinemachineCameraAdapter>();

            var configuration = ScriptableObject.CreateInstance<CameraConfiguration>();
            configuration.Bounds.Enabled = true;
            configuration.Bounds.MinX = -10f;
            configuration.Bounds.MaxX = 10f;
            configuration.Bounds.MinY = -5f;
            configuration.Bounds.MaxY = 5f;
            SetField(controller, "_configuration", configuration);

            SetField(adapter, "_controller", controller);
            SetField(adapter, "_virtualCamera", vcam);
            SetField(adapter, "_confiner", confiner);

            go.SetActive(true);
            yield return null; // let Start() run

            Assert.IsNotNull(confiner.m_BoundingShape2D, "Confiner2D should have a generated BoxCollider2D bounding shape.");
            var collider = confiner.m_BoundingShape2D as BoxCollider2D;
            Assert.IsNotNull(collider);
            Assert.AreEqual(new Vector2(20f, 10f), collider.size);

            Object.Destroy(go);
            Object.Destroy(collider.gameObject);
            Object.DestroyImmediate(configuration);
        }
    }
}
