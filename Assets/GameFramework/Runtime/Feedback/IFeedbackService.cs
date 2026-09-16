using GameFramework.Runtime.Services;

namespace GameFramework.Feedback
{
    /// <summary>
    /// Player feedback abstraction: Game Feature → <see cref="IFeedbackService"/> →
    /// <see cref="IHapticProvider"/> (platform) / <see cref="Audio.IAudioService"/> (for preset
    /// audio). Respects the player's haptics-enabled setting — game code never has to check it.
    /// </summary>
    public interface IFeedbackService : IGameService
    {
        /// <summary>Mirrors the "Feedback.HapticsEnabled" setting. Read-only here — change it via
        /// <c>Settings.ISettingsService.Set</c>, the same as any other setting.</summary>
        bool HapticsEnabled { get; }

        /// <summary>No-ops if <see cref="HapticsEnabled"/> is false or the active
        /// <see cref="IHapticProvider"/> doesn't support haptics on this platform.</summary>
        void TriggerHaptic(HapticStrength strength);

        /// <summary>Triggers a preset's configured haptic and/or audio cue.</summary>
        void TriggerPreset(FeedbackPresetAsset preset);
    }
}
