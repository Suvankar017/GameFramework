using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Audio
{
    /// <summary>
    /// Framework audio playback: Game Feature → <see cref="IAudioService"/> →
    /// <see cref="AudioCueAsset"/> (configuration) → pooled <c>AudioSource</c> playback. Category
    /// volumes are persisted through <c>Settings.ISettingsService</c> (keys under the "Audio"
    /// category) and take effect on already-playing sounds immediately, with no restart required.
    /// </summary>
    public interface IAudioService : IGameService
    {
        /// <summary>Plays <paramref name="cue"/> from the pool. Returns
        /// <see cref="NullAudioHandle"/> (never null) if the cue has no clips, the voice pool is
        /// exhausted, or the cue's own limiting (<see cref="AudioCueAsset.MaxConcurrentInstances"/>/
        /// <see cref="AudioCueAsset.MinRetriggerInterval"/>) suppresses this call.</summary>
        IAudioHandle Play(AudioCueAsset cue, Vector3? worldPosition = null);

        /// <summary>Plays music on a dedicated slot (not part of the general SFX/UI/Voice pool).
        /// If music is already playing, it crossfades: the new track fades in while the old one
        /// fades out over <paramref name="crossfadeSeconds"/>.</summary>
        void PlayMusic(AudioCueAsset cue, float crossfadeSeconds = 0f);

        void StopMusic(float fadeOutSeconds = 0f);

        /// <summary>Stops every currently playing sound, optionally restricted to one category.
        /// Music is only affected if <paramref name="category"/> is <see cref="AudioCategory.Music"/>
        /// or null.</summary>
        void StopAll(AudioCategory? category = null);

        /// <summary>0..1, persisted via Settings. Applies to every category.</summary>
        float MasterVolume { get; set; }

        void SetCategoryVolume(AudioCategory category, float volume01);
        float GetCategoryVolume(AudioCategory category);

        bool IsMuted { get; set; }
    }
}
