using GameFramework.Presentation.Configs;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Pure fade-in/hold/fade-out state machine behind <see cref="PresentationService"/>'s screen
    /// channel - kept Unity-lifecycle-free for the same testability reason as
    /// <see cref="CameraShakeState"/>. <see cref="PresentationService"/> owns the actual overlay
    /// <c>UnityEngine.UI.Image</c> (parented under the existing <c>UI.IUIService</c> layer root,
    /// never a new <c>Canvas</c> - CLAUDE.md's Phase 10 brief, section 16) and applies
    /// <see cref="CurrentAlpha"/>/<see cref="Color"/> to it every tick.
    ///
    /// This channel is exclusive (see <see cref="PresentationService"/>'s remarks on composable vs.
    /// exclusive channels): only one screen effect plays at a time. A new request only interrupts
    /// the current one if its priority is greater than or equal to it (CLAUDE.md's Phase 10 brief,
    /// section 20) - a lower-priority request while a higher-priority effect is still fading is
    /// silently dropped, never queued.
    ///
    /// Note: flat <c>GameFramework.Presentation</c> namespace even though this file lives under
    /// <c>Screen/</c> - see <see cref="ICameraFeedbackDriver"/>'s remarks (a nested <c>.Screen</c>
    /// segment would shadow <see cref="UnityEngine.Screen"/>).
    /// </summary>
    internal sealed class ScreenEffectController
    {
        private enum Phase
        {
            Idle,
            FadeIn,
            Hold,
            FadeOut
        }

        private Phase _phase = Phase.Idle;
        private float _elapsed;
        private float _fadeInSeconds;
        private float _holdSeconds;
        private float _fadeOutSeconds;
        private float _peakAlpha;
        private FeedbackPriority _priority;

        public bool IsActive => _phase != Phase.Idle;
        public Color Color { get; private set; }
        public float CurrentAlpha { get; private set; }

        /// <summary>Starts a new screen effect, or ignores it if a strictly-higher-priority one is
        /// currently playing. Returns whether it was accepted.</summary>
        public bool TryPlay(ScreenEffectFeedbackConfig config, float intensity, FeedbackPriority priority)
        {
            if (IsActive && priority < _priority)
            {
                return false;
            }

            _fadeInSeconds = Mathf.Max(0f, config.FadeInSeconds);
            _holdSeconds = Mathf.Max(0f, config.HoldSeconds);
            _fadeOutSeconds = Mathf.Max(0f, config.FadeOutSeconds);
            _peakAlpha = Mathf.Clamp01(intensity) * config.Color.a;
            Color = config.Color;
            _priority = priority;
            _elapsed = 0f;

            if (_fadeInSeconds > 0f)
            {
                _phase = Phase.FadeIn;
                CurrentAlpha = 0f;
            }
            else if (_holdSeconds > 0f)
            {
                _phase = Phase.Hold;
                CurrentAlpha = _peakAlpha;
            }
            else
            {
                _phase = Phase.FadeOut;
                CurrentAlpha = _peakAlpha;
            }

            return true;
        }

        public void Cancel()
        {
            _phase = Phase.Idle;
            CurrentAlpha = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_phase == Phase.Idle)
            {
                return;
            }

            _elapsed += deltaTime;

            switch (_phase)
            {
                case Phase.FadeIn:
                    if (_elapsed >= _fadeInSeconds)
                    {
                        CurrentAlpha = _peakAlpha;
                        _elapsed = 0f;
                        _phase = _holdSeconds > 0f ? Phase.Hold : Phase.FadeOut;
                    }
                    else
                    {
                        CurrentAlpha = Mathf.Lerp(0f, _peakAlpha, _elapsed / _fadeInSeconds);
                    }
                    break;

                case Phase.Hold:
                    CurrentAlpha = _peakAlpha;
                    if (_elapsed >= _holdSeconds)
                    {
                        _elapsed = 0f;
                        _phase = Phase.FadeOut;
                    }
                    break;

                case Phase.FadeOut:
                    if (_fadeOutSeconds <= 0f || _elapsed >= _fadeOutSeconds)
                    {
                        CurrentAlpha = 0f;
                        _phase = Phase.Idle;
                    }
                    else
                    {
                        CurrentAlpha = Mathf.Lerp(_peakAlpha, 0f, _elapsed / _fadeOutSeconds);
                    }
                    break;
            }
        }
    }
}
