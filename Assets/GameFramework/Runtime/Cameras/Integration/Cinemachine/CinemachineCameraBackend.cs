using System.Collections.Generic;
using Cinemachine;
using GameFramework.Cameras;
using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Cameras.CinemachineIntegration
{
    /// <summary>
    /// The one per-scene component that actually drives Cinemachine on behalf of
    /// <see cref="ICameraService"/> - attach alongside <c>CinemachineBrain</c> on the same GameObject
    /// as the real render <see cref="Camera"/> (the Cinemachine-backed sibling of
    /// <see cref="Cameras.CameraDriver"/>; use one or the other per scene, never both, for the same
    /// camera). Reacts to <see cref="ICameraService.ActiveCameraChanged"/> by mapping the resolved
    /// active camera onto real Cinemachine <see cref="CinemachineVirtualCamera.Priority"/> - it never
    /// blends/writes the Unity Camera transform itself; <c>CinemachineBrain</c> does that entirely on
    /// its own (CLAUDE.md's Phase 11 Cinemachine brief, Rule 5/39).
    ///
    /// Purely event-driven for activation/target changes (no per-frame polling - CLAUDE.md's own Tick
    /// Rules: "can this be event driven?"); the only per-frame work is reading the *currently active*
    /// adapter's <see cref="Cameras.CameraController.ComputePose"/> and writing its zoom half to the
    /// vcam's lens (see <see cref="CinemachineCameraAdapter.ApplyZoom"/>) - the one channel Cinemachine
    /// does not solve for us, and gameplay still zooms through the same
    /// <see cref="Cameras.CameraController.SetZoom"/> call it would for the non-Cinemachine path.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineBrain))]
    public sealed class CinemachineCameraBackend : MonoBehaviour
    {
        [Tooltip("Use unscaled time for zoom damping (e.g. a menu/UI camera that must keep moving " +
            "while gameplay is paused). Most gameplay cameras leave this off so zoom naturally " +
            "freezes while ITimeService.IsPaused is true - mirrors CameraDriver's own default.")]
        [SerializeField] private bool _useUnscaledTime;

        [Tooltip("When enabled, this backend applies Default Blend to the CinemachineBrain once at " +
            "startup - a convenience so a scene's blend policy can be authored alongside the rest of " +
            "this framework's camera setup instead of hunting for the Brain component. Leave off to " +
            "keep whatever is already authored on the Brain (CLAUDE.md's Phase 11 Cinemachine brief, " +
            "section 31: reuse existing Cinemachine Brain blend settings, don't duplicate them).")]
        [SerializeField] private bool _overrideDefaultBlend;

        [SerializeField] private CinemachineBlendDefinition _defaultBlend;

        private readonly Dictionary<CameraController, CinemachineCameraAdapter> _adaptersByController =
            new Dictionary<CameraController, CinemachineCameraAdapter>();

        private CinemachineBrain _brain;
        private ITimeService _time;
        private ICameraService _service;
        private IEventService _events;

        private IEventSubscription _targetChangedSubscription;
        private IEventSubscription _resetSubscription;

        private CinemachineCameraAdapter _activeAdapter;

        private void Awake()
        {
            _brain = GetComponent<CinemachineBrain>();

            if (_overrideDefaultBlend)
            {
                _brain.m_DefaultBlend = _defaultBlend;
            }
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
            _targetChangedSubscription?.Dispose();
            _targetChangedSubscription = null;
            _resetSubscription?.Dispose();
            _resetSubscription = null;

            if (_service != null)
            {
                _service.ActiveCameraChanged -= OnActiveCameraChanged;
            }

            _service = null;
            _activeAdapter = null;
        }

        /// <summary>Called by <see cref="CinemachineCameraAdapter.OnEnable"/> - the same explicit
        /// self-registration pattern <see cref="Cameras.CameraController"/> already uses with
        /// <see cref="ICameraService"/>. Keyed by <see cref="CameraController"/>, never
        /// <see cref="CameraId"/>: an adapter can enable before its controller has finished binding
        /// to <see cref="ICameraService"/> (and therefore before it has a valid <see cref="CameraId"/>),
        /// so <see cref="CameraId"/> is always resolved fresh, on demand, through
        /// <see cref="ICameraService.GetController"/> instead.</summary>
        internal void Register(CinemachineCameraAdapter adapter)
        {
            if (adapter.Controller != null)
            {
                _adaptersByController[adapter.Controller] = adapter;
            }
        }

        internal void Unregister(CinemachineCameraAdapter adapter)
        {
            if (adapter.Controller != null)
            {
                _adaptersByController.Remove(adapter.Controller);
            }

            if (_activeAdapter == adapter)
            {
                _activeAdapter = null;
            }
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

            if (_activeAdapter == null)
            {
                return;
            }

            using (new ProfileScope(ProfilingCategory.Cameras))
            {
                float deltaTime = _useUnscaledTime
                    ? (_time != null ? _time.UnscaledDeltaTime : UnityEngine.Time.unscaledDeltaTime)
                    : (_time != null ? _time.ScaledDeltaTime : UnityEngine.Time.deltaTime);

                // CameraController.ComputePose already applies its own damping/reduce-motion/
                // Camera.ReduceMotion handling (it binds ISettingsService itself when it registers
                // with ICameraService) - this backend only needs the zoom half of the result.
                CameraPose pose = _activeAdapter.Controller.ComputePose(deltaTime);
                _activeAdapter.ApplyZoom(pose);
            }
        }

        private void BindServices()
        {
            IServiceRegistry registry = GameBootstrapper.Instance.Services;
            registry.TryGet(out _time);

            if (!registry.TryGet(out _service))
            {
                return; // ICameraService not registered in this game's bootstrapper - nothing to drive.
            }

            registry.TryGet(out _events);
            if (_events != null)
            {
                _targetChangedSubscription = _events.Subscribe<CameraTargetChangedEvent>(OnCameraTargetChanged);
                _resetSubscription = _events.Subscribe<CameraResetEvent>(OnCameraReset);
            }

            _service.ActiveCameraChanged += OnActiveCameraChanged;

            // Apply whatever is already active (e.g. CameraController._activateOnRegister already ran
            // for a camera registered before this backend finished binding) rather than waiting for the
            // next change.
            OnActiveCameraChanged(CameraId.None, _service.ActiveCameraId);
        }

        private void OnActiveCameraChanged(CameraId previous, CameraId current)
        {
            if (_activeAdapter != null)
            {
                _activeAdapter.SetActive(false);
                _activeAdapter = null;
            }

            CinemachineCameraAdapter adapter = FindAdapter(current);
            if (adapter == null)
            {
                return; // The newly-active camera has no Cinemachine backend - nothing for this component to do.
            }

            adapter.SetActive(true);
            adapter.ApplyTarget();
            adapter.ApplyZoom(adapter.Controller.ComputePose(0f)); // sync the lens immediately, don't wait for next LateUpdate.
            _activeAdapter = adapter;
        }

        private void OnCameraTargetChanged(CameraTargetChangedEvent evt)
        {
            FindAdapter(evt.Id)?.ApplyTarget();
        }

        private void OnCameraReset(CameraResetEvent evt)
        {
            CinemachineCameraAdapter adapter = FindAdapter(evt.Id);
            adapter?.ApplyZoom(adapter.Controller.CurrentPose); // Controller.Snap() already ran by the time this event fires.
        }

        private CinemachineCameraAdapter FindAdapter(CameraId id)
        {
            if (!id.IsValid || _service == null)
            {
                return null;
            }

            CameraController controller = _service.GetController(id);
            return controller != null && _adaptersByController.TryGetValue(controller, out CinemachineCameraAdapter adapter)
                ? adapter
                : null;
        }
    }
}
