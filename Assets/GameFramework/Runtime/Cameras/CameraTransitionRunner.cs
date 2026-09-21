using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Pure blend used by <see cref="CameraDriver"/> whenever the currently active
    /// <see cref="CameraController"/> changes (activation, override push/pop) - blends from the pose
    /// actually applied last frame toward the newly active controller's live computed pose each
    /// frame, rather than a fixed start/end pair, so an interrupted or re-interrupted transition
    /// (CLAUDE.md's Phase 11 brief, section 19: "interruptible, cancelable") always blends from
    /// wherever the camera visually is right now.
    /// </summary>
    internal sealed class CameraTransitionRunner
    {
        private CameraPose _from;
        private float _duration;
        private AnimationCurve _ease;
        private float _elapsed;
        private bool _active;

        public bool IsActive => _active;

        public void Begin(CameraPose from, CameraTransitionSettings settings)
        {
            _from = from;
            _duration = settings != null ? Mathf.Max(0f, settings.Duration) : 0f;
            _ease = settings?.Ease;
            _elapsed = 0f;
            _active = _duration > 0f;
        }

        public CameraPose Tick(CameraPose target, float deltaTime)
        {
            if (!_active)
            {
                return target;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float eased = _ease != null && _ease.length > 0 ? Mathf.Clamp01(_ease.Evaluate(t)) : t;

            if (t >= 1f)
            {
                _active = false;
            }

            return CameraPose.Lerp(_from, target, eased);
        }

        /// <summary>Ends the transition immediately - used when a target camera is unregistered or
        /// destroyed mid-blend (CLAUDE.md's Phase 11 brief, section 19: "safe when target
        /// disappears").</summary>
        public void Cancel() => _active = false;
    }
}
