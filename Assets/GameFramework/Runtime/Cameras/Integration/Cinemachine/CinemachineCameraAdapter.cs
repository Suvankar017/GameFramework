using Cinemachine;
using GameFramework.Cameras;
using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras.CinemachineIntegration
{
    /// <summary>
    /// Pairs one <see cref="Cameras.CameraController"/> (GameFramework identity/registration/target/
    /// override ownership - completely unchanged) with one <see cref="CinemachineVirtualCamera"/>
    /// (Cinemachine's actual follow/framing/lens solving) - the adapter boundary CLAUDE.md's Phase 11
    /// Cinemachine brief, section 8 asks for. Attach alongside a <c>CinemachineVirtualCamera</c> on a
    /// camera rig prefab; <see cref="CinemachineCameraBackend"/> is the one per-scene component that
    /// actually drives activation/target/zoom through this adapter - the same "framework owns
    /// orchestration, Cinemachine owns solving" split <see cref="Cameras.CameraController"/>/
    /// <see cref="Cameras.CameraDriver"/> already establish for the non-Cinemachine path.
    ///
    /// Deliberately does not touch <c>transform</c> - position/rotation solving belongs entirely to
    /// <see cref="_virtualCamera"/>'s own Follow/LookAt/Composer/FramingTransposer pipeline and
    /// <c>CinemachineBrain</c>'s blending (Rule 5/39 of the brief: never fight Cinemachine by writing
    /// the Unity Camera transform directly). The one channel this adapter *does* still surface is
    /// zoom/lens (<see cref="ApplyZoom"/>) - Cinemachine has no time-based "damp this vcam's own lens
    /// toward a target value" concept, and gameplay already zooms a Cinemachine-backed camera exactly
    /// the way it zooms a non-Cinemachine one: calling the existing, public
    /// <see cref="Cameras.CameraController.SetZoom"/> directly on the controller it holds a reference
    /// to. So rather than adding a second, competing zoom entry point, this adapter reuses
    /// <see cref="Cameras.CameraController.ComputePose"/> itself (already public, already damped,
    /// already reduce-motion-aware) and reads only its <c>OrthographicSize</c>/<c>FieldOfView</c> -
    /// <c>CinemachineCameraBackend</c> discards the position/rotation half of that same call, since
    /// Cinemachine already owns those.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CinemachineCameraAdapter : MonoBehaviour
    {
        [Tooltip("The GameFramework camera identity/registration this vcam backs. Still owns " +
            "RegisterCamera/target assignment/override participation, and SetZoom, exactly as it " +
            "would for the non-Cinemachine CameraDriver path - this adapter only adds a Cinemachine " +
            "backend on top.")]
        [SerializeField] private CameraController _controller;

        [Tooltip("The Cinemachine virtual camera this adapter drives. Its own Follow/LookAt/Body/Aim " +
            "components remain the source of truth for framing, dead zone, and damping - author them " +
            "directly on this vcam (CLAUDE.md's Phase 11 Cinemachine brief, section 64).")]
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;

        [Tooltip("This vcam's Priority while NOT the GameFramework-resolved active camera. Kept " +
            "separate from CameraController's own Priority field, which stays informational-only for " +
            "the non-Cinemachine path (see CameraService's remarks) - this one is real Cinemachine " +
            "policy, mapped by CinemachineCameraBackend, never compared directly by gameplay code.")]
        [SerializeField] private int _basePriority = 10;

        [Tooltip("Optional. If assigned and its Bounding Shape 2D is not already authored, this " +
            "adapter generates a runtime BoxCollider2D from CameraController.Configuration.Bounds and " +
            "assigns it - translating this framework's existing world-space rect into what Confiner2D " +
            "needs, never reimplementing confinement math (CLAUDE.md's Phase 11 Cinemachine brief, " +
            "section 23). Leave the Bounding Shape 2D already set on the prefab to keep it authoritative.")]
        [SerializeField] private CinemachineConfiner2D _confiner;

        [Tooltip("The scene's one CinemachineCameraBackend - this adapter self-registers with it in " +
            "OnEnable/unregisters in OnDisable, the same explicit-registration pattern " +
            "Cameras.CameraController already uses with ICameraService (CLAUDE.md's Phase 11 " +
            "Cinemachine brief, section 12: prefer explicit registration over scene scanning).")]
        [SerializeField] private CinemachineCameraBackend _backend;

        /// <summary>How much higher than <see cref="_basePriority"/> this vcam's Priority is set while
        /// it is the GameFramework-resolved active camera - large enough to outrank any other
        /// registered adapter's own <see cref="_basePriority"/> regardless of authored value, so
        /// activation is never ambiguous (CLAUDE.md's Phase 11 Cinemachine brief, section 15).</summary>
        internal const int ActivePriorityBoost = 1000;

        public CameraId Id => _controller != null ? _controller.Id : CameraId.None;
        public CameraController Controller => _controller;
        public CinemachineVirtualCamera VirtualCamera => _virtualCamera;
        public int BasePriority => _basePriority;
        public CinemachineConfiner2D Confiner => _confiner;

        /// <summary>Editor-tooling introspection only (e.g. <c>CinemachineCameraSetupValidator</c>) -
        /// gameplay code has no reason to touch the backend directly.</summary>
        public CinemachineCameraBackend Backend => _backend;

        private void Awake()
        {
            if (_controller == null || _virtualCamera == null)
            {
                Debug.LogError($"[Cameras.Cinemachine] '{name}' is missing its Controller or Virtual " +
                    "Camera reference - disabling this adapter.", this);
                enabled = false;
                return;
            }

            _virtualCamera.Priority = _basePriority;
        }

        private void Start()
        {
            SetupConfinerBounds();
            ApplyTarget();
            ApplyZoom(_controller.CurrentPose); // seed the lens from this controller's authored defaults immediately, not just once activated.
        }

        private void OnEnable()
        {
            if (enabled && _backend != null)
            {
                _backend.Register(this);
            }
        }

        private void OnDisable()
        {
            _backend?.Unregister(this);
        }

        /// <summary>Called by <see cref="CinemachineCameraBackend"/> whenever the GameFramework-resolved
        /// active camera changes - maps the framework's activation decision onto real Cinemachine
        /// priority so <c>CinemachineBrain</c> blends to/from this vcam automatically (never a
        /// framework-owned blend/transition system - CLAUDE.md's Phase 11 Cinemachine brief, section 30).</summary>
        internal void SetActive(bool active)
        {
            _virtualCamera.Priority = active ? _basePriority + ActivePriorityBoost : _basePriority;
        }

        /// <summary>Mirrors <see cref="Cameras.CameraController.Target"/> onto this vcam's Follow/LookAt.
        /// Only a <see cref="Cameras.TransformCameraTarget"/> can be mirrored (Cinemachine needs a real
        /// <see cref="Transform"/>); any other <see cref="ICameraTarget"/> implementation clears
        /// Follow/LookAt instead of guessing - a game needing a non-Transform target on a
        /// Cinemachine-backed camera should target a proxy Transform it updates itself.</summary>
        internal void ApplyTarget()
        {
            Transform target = _controller.Target is TransformCameraTarget transformTarget ? transformTarget.Transform : null;
            _virtualCamera.Follow = target;
            _virtualCamera.LookAt = target;
        }

        /// <summary>Writes only <paramref name="pose"/>'s <c>OrthographicSize</c>/<c>FieldOfView</c> -
        /// whichever matches this vcam's own <c>m_Lens.Orthographic</c> - to the lens; the
        /// position/rotation half of <paramref name="pose"/> is deliberately ignored (Cinemachine owns
        /// those). <see cref="CinemachineCameraBackend"/> is the only intended caller, passing the
        /// result of <see cref="Cameras.CameraController.ComputePose"/> for the currently active
        /// adapter each frame.</summary>
        internal void ApplyZoom(CameraPose pose)
        {
            if (_virtualCamera.m_Lens.Orthographic)
            {
                _virtualCamera.m_Lens.OrthographicSize = pose.OrthographicSize;
            }
            else
            {
                _virtualCamera.m_Lens.FieldOfView = pose.FieldOfView;
            }
        }

        private void SetupConfinerBounds()
        {
            if (_confiner == null || _confiner.m_BoundingShape2D != null)
            {
                return; // No confiner assigned, or the prefab already authors one - prefab stays authoritative.
            }

            CameraConfiguration configuration = _controller.Configuration;
            if (configuration == null || !CinemachineBoundsTranslator.TryComputeBoxBounds(configuration.Bounds, out Vector2 center, out Vector2 size))
            {
                return; // Bounds disabled/unauthored - nothing to generate.
            }

            var boundsObject = new GameObject($"{name} Confiner Bounds (Generated)");
            boundsObject.transform.position = new Vector3(center.x, center.y, 0f);

            var collider = boundsObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            _confiner.m_BoundingShape2D = collider;
            _confiner.InvalidateCache();
        }
    }
}
