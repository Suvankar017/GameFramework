using GameFramework.Audio;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Feedback.Tests
{
    /// <summary>Records <see cref="Play"/> calls instead of actually playing anything, so
    /// FeedbackService's "play a preset's audio cue" behavior is testable without a real
    /// AudioService/AudioSource.</summary>
    internal sealed class FakeAudioService : IAudioService
    {
        public AudioCueAsset LastPlayedCue { get; private set; }
        public int PlayCount { get; private set; }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public IAudioHandle Play(AudioCueAsset cue, Vector3? worldPosition = null)
        {
            LastPlayedCue = cue;
            PlayCount++;
            return NullAudioHandle.Instance;
        }

        public void PlayMusic(AudioCueAsset cue, float crossfadeSeconds = 0f)
        {
        }

        public void StopMusic(float fadeOutSeconds = 0f)
        {
        }

        public void StopAll(AudioCategory? category = null)
        {
        }

        public float MasterVolume { get; set; } = 1f;

        public void SetCategoryVolume(AudioCategory category, float volume01)
        {
        }

        public float GetCategoryVolume(AudioCategory category) => 1f;

        public bool IsMuted { get; set; }
    }
}
