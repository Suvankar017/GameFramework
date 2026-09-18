using System.Collections.Generic;
using GameFramework.Feedback;
using GameFramework.Runtime.Services;

namespace GameFramework.Presentation.Tests
{
    internal sealed class FakeFeedbackService : IFeedbackService
    {
        public readonly List<HapticStrength> TriggeredHaptics = new List<HapticStrength>();

        public bool HapticsEnabled { get; set; } = true;

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void TriggerHaptic(HapticStrength strength)
        {
            if (HapticsEnabled)
            {
                TriggeredHaptics.Add(strength);
            }
        }

        public void TriggerPreset(FeedbackPresetAsset preset)
        {
        }
    }
}
