using System;
using UnityEngine;

namespace GameFramework.Presentation.Configs
{
    /// <summary>A brief hit-stop/slow-motion impulse, built entirely on
    /// <c>Runtime.Time.ITimeService.SetTimeScale</c>/<c>ResetTimeScale</c> - never
    /// <c>Time.timeScale</c> directly, and never <c>Pause</c>/<c>Resume</c> (see
    /// <see cref="Time.TimeFeedbackController"/>'s remarks on why this must never touch another
    /// system's pause). This channel is exclusive: only one time effect is active at a time.</summary>
    [Serializable]
    public sealed class TimeFeedbackConfig
    {
        public bool Enabled;

        [Range(0f, 1f)] public float TimeScale = 0.1f;
        [Min(0f)] public float Duration = 0.05f;
    }
}
