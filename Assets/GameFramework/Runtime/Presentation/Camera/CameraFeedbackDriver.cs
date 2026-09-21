using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Reference <see cref="ICameraFeedbackDriver"/> implementation - attach to a game's camera (or
    /// a parent rig transform it controls) and it self-registers with
    /// <see cref="IPresentationService"/>. Applies <see cref="CameraShakeState"/>'s combined offset
    /// on top of whatever wrote this transform's local position earlier in the frame (a follow
    /// script, typically) without ever permanently corrupting that base pose: each frame it first
    /// subtracts the *previous* frame's offset (undoing its own prior contribution) before adding
    /// the new one, so an external camera-follow system's own writes each frame are always read
    /// cleanly (CLAUDE.md's Phase 10 brief, section 12).
    ///
    /// Uses plain <c>LateUpdate</c>, not <c>Performance.Ticking.ITickService</c> - CLAUDE.md's own
    /// Tick Rules call for plain <c>Update</c>/<c>LateUpdate</c> for "anything simple, low-count, or
    /// already working," and a scene has exactly one active camera driver. <c>LateUpdate</c>
    /// specifically (not <c>Update</c>) is what lets this run after a same-frame camera-follow
    /// script, mirroring <see cref="Performance.Ticking.ILateTickable"/>'s own documented purpose
    /// without needing to register with that service at all.
    ///
    /// <c>[DefaultExecutionOrder(100)]</c> guarantees this runs after any default-order (0)
    /// component, not only <c>Cameras.CameraDriver</c> (already earlier via its own
    /// <c>[DefaultExecutionOrder(-100)]</c>) - specifically so it also composes correctly with
    /// Cinemachine's <c>CinemachineBrain</c>, which writes this same transform in its own
    /// default-order <c>LateUpdate</c> when a game uses Phase 11's Cinemachine integration instead
    /// of <c>CameraDriver</c>. Whichever one owns the base pose this frame, this driver's own
    /// subtract-previous-then-add-new offset pattern below reads it cleanly either way, with zero
    /// compile-time reference to either.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class CameraFeedbackDriver : MonoBehaviour, ICameraFeedbackDriver
    {
        private readonly CameraShakeState _shakeState = new CameraShakeState();

        private ITimeService _time;
        private IPresentationService _presentation;
        private Vector3 _lastAppliedOffset;

        /// <summary>Development-diagnostic only - see <see cref="Gameplay.Pooling.PoolStatistics"/>'s
        /// remarks on this framework's general "read-only diagnostic, not a gameplay API" convention.</summary>
        public int ActiveShakeCount => _shakeState.ActiveCount;

        private void OnEnable()
        {
            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                BindServices();
            }
        }

        private void OnDisable()
        {
            _presentation?.UnregisterCameraDriver(this);
            _shakeState.CancelAll();
            _lastAppliedOffset = Vector3.zero;
        }

        private void BindServices()
        {
            var registry = GameBootstrapper.Instance.Services;
            registry.TryGet(out _time);
            if (registry.TryGet(out _presentation))
            {
                _presentation.RegisterCameraDriver(this);
            }
        }

        public void RequestShake(CameraShakeRequest request) => _shakeState.Add(request);

        public void CancelAll() => _shakeState.CancelAll();

        private void LateUpdate()
        {
            if (_presentation == null && GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                BindServices(); // OnEnable ran before Bootstrap reached Ready - bind on the first tick it's available instead.
            }

            float deltaTime = _time != null ? _time.ScaledDeltaTime : UnityEngine.Time.deltaTime;

            transform.localPosition -= _lastAppliedOffset;
            Vector3 offset = _shakeState.Tick(deltaTime);
            transform.localPosition += offset;
            _lastAppliedOffset = offset;
        }
    }
}
