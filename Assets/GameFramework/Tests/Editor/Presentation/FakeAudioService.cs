using System.Collections.Generic;
using GameFramework.Audio;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Presentation.Tests
{
    internal sealed class FakeAudioHandle : IAudioHandle
    {
        public bool IsPlaying { get; private set; } = true;
        public float? LastSetVolume { get; private set; }
        public float? LastSetPitch { get; private set; }

        public void Stop(float fadeOutSeconds = 0f) => IsPlaying = false;
        public void Pause() => IsPlaying = false;
        public void Resume() => IsPlaying = true;
        public void SetVolume(float volume01) => LastSetVolume = volume01;
        public void SetPitch(float pitch) => LastSetPitch = pitch;
    }

    internal sealed class FakeAudioService : IAudioService
    {
        public readonly List<(AudioCueAsset Cue, Vector3? Position)> PlayedCues = new List<(AudioCueAsset, Vector3?)>();
        public FakeAudioHandle LastHandle { get; private set; }

        public float MasterVolume { get; set; } = 1f;
        public bool IsMuted { get; set; }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public IAudioHandle Play(AudioCueAsset cue, Vector3? worldPosition = null)
        {
            PlayedCues.Add((cue, worldPosition));
            LastHandle = new FakeAudioHandle();
            return LastHandle;
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

        public void SetCategoryVolume(AudioCategory category, float volume01)
        {
        }

        public float GetCategoryVolume(AudioCategory category) => 1f;
    }
}
