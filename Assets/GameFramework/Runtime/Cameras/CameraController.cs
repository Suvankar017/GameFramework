using GameFramework.Cameras.Configuration;
using GameFramework.Cameras.Modes;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Settings;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// The "Camera Target/Intent" authoring point (CLAUDE.md's Phase 11 brief, section 3's
    /// pipeline) - a game attaches this to any GameObject (it does not need its own
    /// <see cref="UnityEngine.Camera"/> component; see <see cref="CameraDriver"/>'s remarks for why)
    /// to describe one camera's target/mode/configuration, and self-registers with
    /// <see cref="ICameraService"/> in <see cref="OnEnable"/> - the same pattern
    /// <c>Presentation.CameraFeedbackDriver</c> already established for its own driver.
    ///
    /// All actual pose math is delegated to the pure <see cref="CameraPoseController"/> so this
    /// MonoBehaviour stays a thin Unity-lifecycle shell (mirrors <c>Presentation.CameraFeedbackDriver</c>
    /// delegating to <c>CameraShakeState</c>).
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Tooltip("Reusable authored settings (mode, follow/target-look, zoom, bounds, transition). " +
            "Shared safely across multiple controllers - runtime state never lives on this asset.")]
        [SerializeField] private CameraConfiguration _configuration;

        [Tooltip("Informational/diagnostic only - see CameraService's remarks. Does not affect which " +
            "camera is active by itself.")]
        [SerializeField] private int _priority;

        [Tooltip("Free-form owner label (e.g. \"Gameplay\", \"BossFight\") - diagnostic only.")]
        [SerializeField] private string _owner;

        [Tooltip("Optional target assigned at startup, wrapped in a TransformCameraTarget. Runtime " +
            "code can replace it at any time via SetTarget/SetTargetTransform.")]
        [SerializeField] private Transform _initialTarget;

        [Tooltip("Must match the actual render Camera's Projection - determines whether this " +
            "controller's zoom/bounds operate on orthographic size or field of view.")]
        [SerializeField] private bool _orthographic = true;

        [Min(0.01f)]
        [SerializeField] private float _aspect = 16f / 9f;

        [Tooltip("Register this camera and immediately Activate() it as the base camera once bound. " +
            "Turn off for a camera a game will Activate()/PushOverride() explicitly later (e.g. a " +
            "boss-fight camera that should stay inactive until needed).")]
        [SerializeField] private bool _activateOnRegister = true;

        private readonly CameraPoseController _poseController = new CameraPoseController();
        private ManualCameraMode _manualMode;

        private ICameraTarget _target;
        private ICameraService _service;
        private ISettingsService _settings;
        private CameraId _id;

        public CameraId Id => _id;
        public CameraConfiguration Configuration => _configuration;
        public int Priority => _priority;
        public string Owner => _owner;
        public bool HasTarget => _target != null && _target.IsValid;
        public CameraPose CurrentPose => _poseController.CurrentPose;

        /// <summary>The target most recently assigned via <see cref="SetTarget"/>/<see cref="SetTargetTransform"/>
        /// (or the Inspector-authored <c>_initialTarget</c>), regardless of validity - an external
        /// integration (e.g. a Cinemachine adapter reacting to <see cref="CameraTargetChangedEvent"/>)
        /// reads this to mirror the assignment onto its own backend rather than requiring a second,
        /// competing "set the target" entry point.</summary>
        public ICameraTarget Target => _target;

        private void Awake()
        {
            ICameraMode mode = _configuration != null ? _configuration.CreateMode() : new StaticCameraMode();
            _manualMode = mode as ManualCameraMode; // null unless CameraModeKind.Manual - fine, SetManualPose no-ops until SetMode(Manual)

            if (_initialTarget != null)
            {
                _target = new TransformCameraTarget(_initialTarget);
            }

            CameraZoomSettings zoom = _configuration != null ? _configuration.Zoom : null;
            CameraBoundsSettings bounds = _configuration != null ? _configuration.Bounds : null;

            _poseController.Initialize(mode, zoom, bounds, _orthographic, _aspect, ComputeInitialPose(zoom));
            _poseController.SetTarget(_target);
        }

        private void OnEnable()
        {
            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                BindService();
            }
        }

        private void OnDisable()
        {
            _service?.UnregisterCamera(_id);
            _service = null;
            _id = CameraId.None;
        }

        private void Update()
        {
            if (_service == null && GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                BindService(); // OnEnable ran before Bootstrap reached Ready - bind on the first frame it's available instead.
            }
        }

        private void BindService()
        {
            Runtime.Services.IServiceRegistry registry = GameBootstrapper.Instance.Services;
            if (!registry.TryGet(out ICameraService service))
            {
                return;
            }

            _service = service;
            registry.TryGet(out _settings);
            _id = service.RegisterCamera(this, _priority, _owner);

            if (_activateOnRegister)
            {
                service.Activate(_id);
            }
        }

        public void SetTarget(ICameraTarget target)
        {
            _target = target;
            _poseController.SetTarget(target);
        }

        public void SetTargetTransform(Transform target) => SetTarget(target != null ? new TransformCameraTarget(target) : null);

        public void ClearTarget() => SetTarget(null);

        public void SetMode(ICameraMode mode)
        {
            _manualMode = mode as ManualCameraMode;
            _poseController.SetMode(mode);
        }

        /// <summary>Supplies this frame's pose while the current mode is <see cref="ManualCameraMode"/>
        /// (CLAUDE.md's Phase 11 brief, section 8) - a no-op if a different mode is active.</summary>
        public void SetManualPose(CameraPose pose) => _manualMode?.SetPose(pose);

        public void SetZoom(float value) => _poseController.SetZoom(value);

        /// <summary>Resets this controller's pose to its target/mode's current desired pose
        /// immediately, clearing any in-progress damping (CLAUDE.md's Phase 11 brief, section 27).</summary>
        public void Snap() => _poseController.Snap();

        /// <summary>Advances this controller's own pose one tick. Only meaningful while this is the
        /// <see cref="ICameraService"/>'s active camera - <see cref="CameraDriver"/> is the only
        /// intended caller.</summary>
        public CameraPose ComputePose(float deltaTime)
        {
            bool reduceMotion = _settings != null && _settings.TryGet(CameraService.ReduceMotionSettingKey, out bool value) && value;
            return _poseController.Tick(deltaTime, reduceMotion);
        }

        /// <summary>Seeds this controller's very first pose from its own authored transform,
        /// deliberately never from <see cref="_target"/> - a target's position is only ever
        /// reflected on the axes <see cref="Modes.FollowCameraMode"/> (or another mode) actually
        /// tracks, once real ticking begins; seeding directly from the target here would corrupt any
        /// axis this controller does *not* follow (e.g. a fixed camera depth) with the target's
        /// unrelated value on that same axis.</summary>
        private CameraPose ComputeInitialPose(CameraZoomSettings zoom)
        {
            float orthographicSize = zoom != null ? zoom.DefaultOrthographicSize : 5f;
            float fieldOfView = zoom != null ? zoom.DefaultFieldOfView : 60f;
            return new CameraPose(transform.position, transform.rotation, orthographicSize, fieldOfView);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_configuration == null)
            {
                return;
            }

            Vector3 center = Application.isPlaying ? CurrentPose.Position : transform.position;

            if (_configuration.Mode == CameraModeKind.Follow)
            {
                CameraFollowSettings follow = _configuration.Follow;
                if (follow.DeadZoneSize.sqrMagnitude > 0f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(center, new Vector3(follow.DeadZoneSize.x, follow.DeadZoneSize.y, 0f));
                }

                if (follow.UseSoftZone && follow.SoftZoneSize.sqrMagnitude > 0f)
                {
                    Gizmos.color = new Color(1f, 0.6f, 0f);
                    Gizmos.DrawWireCube(center, new Vector3(follow.DeadZoneSize.x + follow.SoftZoneSize.x * 2f,
                        follow.DeadZoneSize.y + follow.SoftZoneSize.y * 2f, 0f));
                }
            }

            CameraBoundsSettings bounds = _configuration.Bounds;
            if (bounds.Enabled)
            {
                Gizmos.color = Color.cyan;
                Vector3 boundsCenter = new Vector3((bounds.MinX + bounds.MaxX) * 0.5f, (bounds.MinY + bounds.MaxY) * 0.5f, center.z);
                Vector3 boundsSize = new Vector3(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY, 0f);
                Gizmos.DrawWireCube(boundsCenter, boundsSize);
            }

            if (HasTarget)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(center, _target.Position);
            }
        }
#endif
    }
}
