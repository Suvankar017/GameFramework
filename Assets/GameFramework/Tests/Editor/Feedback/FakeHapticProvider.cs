using GameFramework.Feedback;

namespace GameFramework.Feedback.Tests
{
    internal sealed class FakeHapticProvider : IHapticProvider
    {
        public bool IsSupported { get; set; } = true;
        public int TriggerCount { get; private set; }
        public HapticStrength LastStrength { get; private set; }

        public void Trigger(HapticStrength strength)
        {
            TriggerCount++;
            LastStrength = strength;
        }
    }
}
