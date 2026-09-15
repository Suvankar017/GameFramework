using GameFramework.Audio;
using UnityEngine;

namespace GameFramework.Feedback
{
    /// <summary>
    /// A reusable, game-defined bundle of feedback (e.g. "ButtonPress", "Reward", "Error") that
    /// coordinates existing systems — haptics here, and optionally an <see cref="AudioCueAsset"/>
    /// played through <see cref="Audio.IAudioService"/> — rather than duplicating audio playback.
    /// Presets themselves are never named by the framework; only the mechanism is provided.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Feedback/Feedback Preset", fileName = "FeedbackPreset")]
    public sealed class FeedbackPresetAsset : ScriptableObject
    {
        public bool TriggerHaptic = true;
        public HapticStrength Haptic = HapticStrength.Light;

        [Tooltip("Optional - left empty if this preset should only trigger haptics.")]
        public AudioCueAsset AudioCue;
    }
}
