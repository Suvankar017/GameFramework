namespace GameFramework.Feedback
{
    /// <summary>Platform seam between <see cref="IFeedbackService"/> and the actual device haptic
    /// call. Never invoked directly by game code — go through <see cref="IFeedbackService"/>,
    /// which also applies the player's haptics-enabled setting.</summary>
    public interface IHapticProvider
    {
        /// <summary>False on platforms/devices with no meaningful haptic output (Editor, desktop
        /// standalone) — <see cref="Trigger"/> is still safe to call in that case; it just no-ops.</summary>
        bool IsSupported { get; }

        void Trigger(HapticStrength strength);
    }
}
