using System.Collections;
using GameFramework.Cameras;
using GameFramework.Presentation;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Samples.Phase11Demo
{
    /// <summary>
    /// Drives the Phase 11 demonstration scene: a movable Player target followed by a Follow-mode
    /// gameplay camera (dead zone + world bounds + zoom, see the "GameplayCamera"
    /// <c>CameraConfiguration</c> asset under <c>Content/</c>), a temporary "Boss" camera override,
    /// and Phase 10 camera-feedback (shake) layered on top via <see cref="IPresentationService"/> -
    /// the same "register content after Ready" pattern every other phase's demo controller follows.
    /// Not part of the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase11DemoController : MonoBehaviour
    {
        [Tooltip("The Follow target - moved by WASD/arrow keys.")]
        [SerializeField] private Transform _player;

        [Tooltip("CameraController on the Main Camera (Follow mode, dead zone, bounds, zoom).")]
        [SerializeField] private CameraController _gameplayCamera;

        [Tooltip("CameraController with no physical Camera of its own - Static mode, a fixed " +
            "vantage point pushed as a temporary override.")]
        [SerializeField] private CameraController _bossCamera;

        [Tooltip("Assigned from Content/CameraShake.asset - Camera channel only.")]
        [SerializeField] private FeedbackDefinition _cameraShake;

        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _zoomStep = 1f;

        private ICameraService _cameras;
        private IPresentationService _presentation;
        private ICameraOverrideHandle _bossOverride;
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
            Debug.Log("[Phase11Demo] Ready. WASD/Arrows = move player, 1 = push Boss camera override, " +
                "2 = pop it, Z/X = zoom out/in, S = camera shake, R = reset gameplay camera.");
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h != 0f || v != 0f)
            {
                _player.position += new Vector3(h, v, 0f).normalized * (_moveSpeed * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) && _bossOverride == null)
            {
                _bossOverride = _cameras.PushOverride(_bossCamera.Id);
                Debug.Log("[Phase11Demo] Boss camera override pushed.");
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) && _bossOverride != null)
            {
                _bossOverride.Release();
                _bossOverride = null;
                Debug.Log("[Phase11Demo] Boss camera override released - back to gameplay camera.");
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                _currentZoom += _zoomStep;
                _gameplayCamera.SetZoom(_currentZoom);
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                _currentZoom -= _zoomStep;
                _gameplayCamera.SetZoom(_currentZoom);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                PlayResult result = _presentation.Play(_cameraShake.Id, worldPosition: _player.position);
                Debug.Log($"[Phase11Demo] Play(CameraShake) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _cameras.ResetCamera(_gameplayCamera.Id);
                Debug.Log("[Phase11Demo] Gameplay camera reset.");
            }
        }
    }
}
