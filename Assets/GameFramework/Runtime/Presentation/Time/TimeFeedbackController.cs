using GameFramework.Presentation.Configs;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Pure countdown/priority state machine behind <see cref="PresentationService"/>'s time
    /// channel - decides only *whether* a new hit-stop/slow-motion request should take over and
    /// *when* the current one should end; <see cref="PresentationService"/> is the one place that
    /// actually calls <c>Runtime.Time.ITimeService.SetTimeScale</c>/<c>ResetTimeScale</c>, never
    /// <c>Pause</c>/<c>Resume</c> - a hit-stop must never release another system's (GameFlow's,
    /// Tutorial's, application-background's) gameplay pause (CLAUDE.md's Phase 10 brief, section 19).
    ///
    /// This channel is exclusive, the same replace-if-not-lower-priority policy
    /// <see cref="ScreenEffectController"/> uses: at most one time effect owns the time-scale
    /// override at a time, so a second request while one is active either extends/replaces it
    /// (priority allowing) or is ignored - never two competing owners each assuming they control
    /// when time scale resets.
    ///
    /// Note: flat <c>GameFramework.Presentation</c> namespace even though this file lives under
    /// <c>Time/</c> - see <see cref="ICameraFeedbackDriver"/>'s remarks (a nested <c>.Time</c>
    /// segment would shadow <see cref="UnityEngine.Time"/>).
    /// </summary>
    internal sealed class TimeFeedbackController
    {
        private float _remainingSeconds;
        private FeedbackPriority _priority;

        public bool IsActive => _remainingSeconds > 0f;

        /// <summary>Starts (or replaces) the active time effect. Returns false, leaving the current
        /// effect untouched, if one is already active at a strictly higher priority.</summary>
        public bool TryPlay(TimeFeedbackConfig config, FeedbackPriority priority)
        {
            if (IsActive && priority < _priority)
            {
                return false;
            }

            _remainingSeconds = Mathf.Max(0f, config.Duration);
            _priority = priority;
            return true;
        }

        /// <summary>Advances the countdown by unscaled time (this must never be scaled time - the
        /// whole point of this effect is to modify the scale, so its own duration cannot depend on
        /// it). Returns true on exactly the tick the effect finishes, telling the caller to reset
        /// the time scale now.</summary>
        public bool Tick(float unscaledDeltaTime)
        {
            if (!IsActive)
            {
                return false;
            }

            _remainingSeconds -= unscaledDeltaTime;
            if (_remainingSeconds <= 0f)
            {
                _remainingSeconds = 0f;
                return true;
            }

            return false;
        }

        public void Cancel() => _remainingSeconds = 0f;
    }
}
