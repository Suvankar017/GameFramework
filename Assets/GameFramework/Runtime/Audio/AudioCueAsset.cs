using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Audio
{
    /// <summary>
    /// Playback configuration for one sound — content/config, never runtime state (see
    /// <see cref="AudioService"/> for what actually plays it). Randomization
    /// (<see cref="Clips"/>/volume/pitch ranges) and limiting (<see cref="MaxConcurrentInstances"/>/
    /// <see cref="MinRetriggerInterval"/>) are opt-in via their defaults: one clip and equal
    /// min/max produces fixed playback; zero limits mean unlimited.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Audio/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCueAsset : ScriptableObject
    {
        public AudioCategory Category = AudioCategory.Sfx;
        public List<AudioClip> Clips = new List<AudioClip>();

        [Range(0f, 1f)] public float MinVolume = 1f;
        [Range(0f, 1f)] public float MaxVolume = 1f;
        [Range(0.1f, 3f)] public float MinPitch = 1f;
        [Range(0.1f, 3f)] public float MaxPitch = 1f;

        public bool Loop;
        [Min(0f)] public float FadeInSeconds;
        [Min(0f)] public float FadeOutSeconds;

        [Tooltip("0 = unlimited simultaneous instances of this cue.")]
        [Min(0)] public int MaxConcurrentInstances;

        [Tooltip("0 = no minimum interval between triggers of this cue.")]
        [Min(0f)] public float MinRetriggerInterval;
    }
}
