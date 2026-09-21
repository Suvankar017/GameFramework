using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Pure, damped zoom value - orthographic size or field of view, clamped to
    /// <see cref="CameraZoomSettings"/>'s min/max for whichever projection mode this controller's
    /// camera uses (CLAUDE.md's Phase 11 brief, sections 14-15). Composes independently of
    /// <see cref="ICameraMode"/> so any mode can be zoomed - "zoom changes must compose correctly
    /// with follow/bounds/transitions" (section 14).
    /// </summary>
    internal sealed class CameraZoomController
    {
        private readonly CameraZoomSettings _settings;
        private readonly bool _orthographic;

        private float _current;
        private float _target;
        private float _velocity;

        public CameraZoomController(CameraZoomSettings settings, bool orthographic, float initialValue)
        {
            _settings = settings;
            _orthographic = orthographic;
            _current = initialValue;
            _target = initialValue;
        }

        public float Current => _current;

        public void SetZoom(float value)
        {
            _target = _settings == null ? value : Mathf.Clamp(value, MinValue, MaxValue);
        }

        public float Tick(float deltaTime, bool reduceMotion = false)
        {
            float damping = reduceMotion || _settings == null ? 0f : _settings.ZoomDamping;
            _current = damping <= 0f
                ? _target
                : Mathf.SmoothDamp(_current, _target, ref _velocity, damping, Mathf.Infinity, deltaTime);
            return _current;
        }

        public void Snap()
        {
            _current = _target;
            _velocity = 0f;
        }

        private float MinValue => _orthographic ? _settings.MinOrthographicSize : _settings.MinFieldOfView;
        private float MaxValue => _orthographic ? _settings.MaxOrthographicSize : _settings.MaxFieldOfView;
    }
}
