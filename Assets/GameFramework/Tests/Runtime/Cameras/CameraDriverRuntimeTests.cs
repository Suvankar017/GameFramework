using System.Collections;
using System.Reflection;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameFramework.Cameras.Tests
{
    /// <summary>
    /// Covers <see cref="CameraDriver.LateUpdate"/> actually firing and applying a pose over real
    /// engine frames - EditMode does not run the player loop (no <c>Update</c>/<c>LateUpdate</c>
    /// callbacks), so this genuinely needs Play Mode, the same reasoning
    /// <c>Presentation.Tests.PresentationServiceRuntimeTests</c> already documents for its own
    /// engine-only behavior.
    ///
    /// Bypasses <see cref="Runtime.Bootstrap.GameBootstrapper"/> entirely (a true singleton, risky
    /// to spin up per-test) by reflection-injecting <see cref="CameraDriver"/>'s private
    /// <c>_service</c> field directly - the same technique
    /// <c>Presentation.Tests.PresentationServiceRuntimeTests.SetId</c> already uses for
    /// <c>FeedbackDefinition</c>'s private <c>_id</c> field.
    /// </summary>
    public class CameraDriverRuntimeTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private CameraService _service;
        private GameObject _cameraGameObject;
        private Camera _camera;
        private CameraDriver _driver;

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

            _cameraGameObject = new GameObject("Camera");
            _camera = _cameraGameObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _driver = _cameraGameObject.AddComponent<CameraDriver>();
            InjectService(_driver, _service);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
            if (_cameraGameObject != null)
            {
                Object.Destroy(_cameraGameObject);
            }
        }

        private static void InjectService(CameraDriver driver, ICameraService service)
        {
            FieldInfo field = typeof(CameraDriver).GetField("_service", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(driver, service);
        }

        private CameraController CreateController(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            return go.AddComponent<CameraController>();
        }

        [UnityTest]
        public IEnumerator LateUpdate_ActiveCamera_AppliesPoseToTransform()
        {
            CameraController controller = CreateController("Static", new Vector3(3f, 4f, -10f));
            CameraId id = _service.RegisterCamera(controller);
            _service.Activate(id);

            yield return null;

            Assert.AreEqual(3f, _cameraGameObject.transform.position.x, 0.01f);
            Assert.AreEqual(4f, _cameraGameObject.transform.position.y, 0.01f);

            Object.Destroy(controller.gameObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_AppliesOrthographicSize_FromActiveCameraZoom()
        {
            CameraController controller = CreateController("Zoomed", Vector3.zero);
            CameraId id = _service.RegisterCamera(controller);
            _service.Activate(id);
            controller.SetZoom(9f);

            yield return null;
            yield return null; // allow damping to settle toward the requested zoom

            Assert.AreEqual(9f, _camera.orthographicSize, 0.5f);

            Object.Destroy(controller.gameObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_NoActiveCamera_DoesNotThrow_LeavesTransformUntouched()
        {
            _cameraGameObject.transform.position = new Vector3(1f, 1f, 1f);

            yield return null;

            Assert.AreEqual(new Vector3(1f, 1f, 1f), _cameraGameObject.transform.position);
        }

        [UnityTest]
        public IEnumerator ActiveCameraSwitch_OverrideThenPop_DriverFollowsResolvedActiveCamera()
        {
            CameraController baseController = CreateController("Base", new Vector3(0f, 0f, 0f));
            CameraController overrideController = CreateController("Override", new Vector3(50f, 0f, 0f));
            CameraId baseId = _service.RegisterCamera(baseController);
            CameraId overrideId = _service.RegisterCamera(overrideController);
            _service.Activate(baseId);
            yield return null;

            ICameraOverrideHandle handle = _service.PushOverride(overrideId);
            yield return null;
            yield return null; // allow the (zero-duration default) transition to resolve fully

            Assert.AreEqual(50f, _cameraGameObject.transform.position.x, 1f);

            handle.Release();
            yield return null;
            yield return null;

            Assert.AreEqual(0f, _cameraGameObject.transform.position.x, 1f);

            Object.Destroy(baseController.gameObject);
            Object.Destroy(overrideController.gameObject);
        }
    }
}
