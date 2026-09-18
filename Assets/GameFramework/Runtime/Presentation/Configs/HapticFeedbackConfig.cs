using System;
using GameFramework.Feedback;

namespace GameFramework.Presentation.Configs
{
    /// <summary>Requests <see cref="IFeedbackService.TriggerHaptic"/> - never
    /// <see cref="UnityEngine.Handheld.Vibrate"/> or <see cref="IHapticProvider"/> directly, so the
    /// player's "Feedback.HapticsEnabled" setting and platform support are already respected.
    /// <see cref="HapticStrength"/> is a fixed semantic enum (see its own remarks), so
    /// <see cref="FeedbackRequest.Intensity"/> does not scale it numerically - a documented
    /// limitation, not an oversight.</summary>
    [Serializable]
    public sealed class HapticFeedbackConfig
    {
        public bool Enabled;
        public HapticStrength Strength = HapticStrength.Medium;
    }
}
