using System.Collections;
using GameFramework.Cameras;
using GameFramework.Presentation;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Samples.Phase11Demo
{
    /// <summary>
    /// The Cinemachine counterpart to <see cref="Phase11DemoController"/> - deliberately identical
    /// gameplay-facing calls (<see cref="ICameraService.PushOverride"/>/<see cref="CameraController.SetZoom"/>/
    /// <see cref="ICameraService.ResetCamera"/>/<see cref="IPresentationService.Play"/>) against the
    /// same <c>ICameraService</c>/<c>CameraController</c> types - this script never references
    /// <c>Cinemachine</c> types at all, which is the point: whether <c>_gameplayCamera</c>/
    /// <c>_eventCamera</c> are driven by <c>CameraDriver</c> or a <c>CinemachineCameraAdapter</c> is a
    /// scene-composition detail, invisible from here (CLAUDE.md's Phase 11 Cinemachine brief, Rule 2/9).
    /// Not part of the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase11CinemachineDemoController : MonoBehaviour
    {
        [Tooltip("The Follow target - moved by WASD/arrow keys. Assigned as the gameplay " +
            "CameraController's initial target, which CinemachineCameraAdapter mirrors onto the " +
            "vcam's Follow/LookAt.")]
        [SerializeField] private Transform _player;

        [Tooltip("CameraController paired with a CinemachineVirtualCamera (Framing Transposer: dead " +
            "zone/damping; Confiner2D: world bounds) via CinemachineCameraAdapter.")]
        [SerializeField] private CameraController _gameplayCamera;

        [Tooltip("CameraController paired with a fixed-vantage CinemachineVirtualCamera, pushed as a " +
            "temporary override - CinemachineBrain performs the actual blend, not CameraTransitionRunner.")]
        [SerializeField] private CameraController _eventCamera;

        [Tooltip("Assigned from Content/CameraShake.asset - Camera channel only. Proves Phase 10 " +
            "feedback composes on top of a Cinemachine-driven camera with zero code changes.")]
        [SerializeField] private FeedbackDefinition _cameraShake;

        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _zoomStep = 1f;

        private ICameraService _cameras;
        private IPresentationService _presentation;
        private ICameraOverrideHandle _eventOverride;
        private float _currentZoom = 5f;
        private bool _ready;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _cameras = services.Get<ICameraService>();
            _presentation = services.Get<IPresentationService>();

            _presentation.RegisterDefinition(_cameraShake);

            _ready = true;
            Debug.Log("[Phase11CinemachineDemo] Ready. WASD/Arrows = move player, 1 = push Event " +
                "camera override (Cinemachine blend), 2 = pop it, Z/X = zoom out/in, S = camera " +
                "shake, R = reset gameplay camera.");
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");
            if (h != 0f || v != 0f)
            {
                _player.position += new Vector3(h, v, 0f).normalized * (_moveSpeed * UnityEngine.Time.deltaTime);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) && _eventOverride == null)
            {
                _eventOverride = _cameras.PushOverride(_eventCamera.Id);
                Debug.Log("[Phase11CinemachineDemo] Event camera override pushed - CinemachineBrain blends.");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) && _eventOverride != null)
            {
                _eventOverride.Release();
                _eventOverride = null;
                Debug.Log("[Phase11CinemachineDemo] Event camera override released - back to gameplay camera.");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Z))
            {
                _currentZoom += _zoomStep;
                _gameplayCamera.SetZoom(_currentZoom);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.X))
            {
                _currentZoom -= _zoomStep;
                _gameplayCamera.SetZoom(_currentZoom);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                PlayResult result = _presentation.Play(_cameraShake.Id, worldPosition: _player.position);
                Debug.Log($"[Phase11CinemachineDemo] Play(CameraShake) -> {result}");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _cameras.ResetCamera(_gameplayCamera.Id);
                Debug.Log("[Phase11CinemachineDemo] Gameplay camera reset.");
            }
        }
    }
}
