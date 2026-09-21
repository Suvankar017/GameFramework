using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// The "Camera Driver" layer (CLAUDE.md's Phase 11 brief, section 3's pipeline) - the one
    /// MonoBehaviour per scene that actually writes to a real <see cref="UnityEngine.Camera"/>/
    /// <see cref="Transform"/>. Each frame it asks <see cref="ICameraService"/> for the currently
    /// active <see cref="CameraController"/>, advances that controller's pose, blends through a
    /// <see cref="CameraTransitionRunner"/> when the active controller just changed, and applies the
    /// result. A game attaches this to its actual render Camera GameObject, alongside
    /// <c>Presentation.CameraFeedbackDriver</c> if Phase 10 is also in use - see this class's
    /// <see cref="DefaultExecutionOrderAttribute"/> remarks for how the two compose without either
    /// referencing the other.
    ///
    /// Plain <c>LateUpdate</c>, not <see cref="Performance.Ticking.ITickService"/> - CLAUDE.md's own
    /// Tick Rules call for plain Update/LateUpdate for "anything simple, low-count, or already
    /// working," and a scene has exactly one active camera driver, the same reasoning
    /// <c>Presentation.CameraFeedbackDriver</c> already used for itself. <c>[DefaultExecutionOrder(-100)]</c>
    /// guarantees this runs *before* that driver's own (default-order) <c>LateUpdate</c> regardless
    /// of GameObject/component instantiation order, so <c>CameraFeedbackDriver</c> always reads this
    /// frame's freshly-written base pose (via <c>transform.localPosition</c>) before layering its own
    /// shake offset on top - CLAUDE.md's Phase 11 brief, section 17's "Base Pose -&gt; Constraints -&gt;
    /// Camera Feedback -&gt; Final Camera Transform" composition order, achieved with no compile-time
    /// reference between the two assemblies in either direction (section 18).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public sealed class CameraDriver : MonoBehaviour
    {
        [Tooltip("Use unscaled time for this driver's own pose advancement (e.g. a menu/UI camera " +
            "that must keep moving while gameplay is paused). Most gameplay cameras leave this off " +
            "so following naturally freezes while ITimeService.IsPaused is true.")]
        [SerializeField] private bool _useUnscaledTime;

        private readonly CameraTransitionRunner _transition = new CameraTransitionRunner();

        private Camera _camera;
        private ITimeService _time;
        private ICameraService _service;
        private CameraController _lastActiveController;
        private CameraPose _lastAppliedPose;
        private bool _hasAppliedPose;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                BindServices();
            }
        }

        private void OnDisable()
        {
            _service = null;
            _lastActiveController = null;
            _hasAppliedPose = false;
        }

        private void LateUpdate()
        {
            if (_service == null)
            {
                if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
                {
                    return;
                }

                BindServices(); // OnEnable ran before Bootstrap reached Ready - bind on the first tick it's available instead.
                if (_service == null)
                {
                    return; // ICameraService not registered in this game's bootstrapper - nothing to drive.
                }
            }

            CameraController active = _service.ActiveCamera;
            if (active == null)
            {
                return;
            }

            using (new ProfileScope(ProfilingCategory.Cameras))
            {
                float deltaTime = _useUnscaledTime
                    ? (_time != null ? _time.UnscaledDeltaTime : UnityEngine.Time.unscaledDeltaTime)
                    : (_time != null ? _time.ScaledDeltaTime : UnityEngine.Time.deltaTime);

                if (active != _lastActiveController)
                {
                    CameraPose blendFrom = _hasAppliedPose ? _lastAppliedPose : active.CurrentPose;
                    _transition.Begin(blendFrom, active.Configuration != null ? active.Configuration.DefaultTransition : null);
                    _lastActiveController = active;
                }

                CameraPose computed = active.ComputePose(deltaTime);
                CameraPose final = _transition.Tick(computed, deltaTime);

                ApplyPose(final);
                _lastAppliedPose = final;
                _hasAppliedPose = true;
            }
        }

        private void ApplyPose(CameraPose pose)
        {
            transform.position = pose.Position;
            transform.rotation = pose.Rotation;

            if (_camera.orthographic)
            {
                _camera.orthographicSize = pose.OrthographicSize;
            }
            else
            {
                _camera.fieldOfView = pose.FieldOfView;
            }
        }

        private void BindServices()
        {
            IServiceRegistry registry = GameBootstrapper.Instance.Services;
            registry.TryGet(out _time);
            registry.TryGet(out _service);
        }
    }
}
