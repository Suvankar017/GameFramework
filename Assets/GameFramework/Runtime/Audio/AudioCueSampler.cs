using UnityEngine;

namespace GameFramework.Audio
{
    /// <summary>Pure clip/volume/pitch selection logic for an <see cref="AudioCueAsset"/>,
    /// isolated from <see cref="AudioService"/> so it's deterministically unit-testable via a fake
    /// <see cref="IRandomSource"/>.</summary>
    internal static class AudioCueSampler
    {
        internal static bool TrySelect(AudioCueAsset cue, IRandomSource random, out AudioClip clip, out float volume, out float pitch)
        {
            if (cue == null || cue.Clips == null || cue.Clips.Count == 0)
            {
                clip = null;
                volume = 0f;
                pitch = 1f;
                return false;
            }

            clip = cue.Clips.Count == 1 ? cue.Clips[0] : cue.Clips[random.NextInt(0, cue.Clips.Count)];
            volume = random.NextFloat(Mathf.Min(cue.MinVolume, cue.MaxVolume), Mathf.Max(cue.MinVolume, cue.MaxVolume));
            pitch = random.NextFloat(Mathf.Min(cue.MinPitch, cue.MaxPitch), Mathf.Max(cue.MinPitch, cue.MaxPitch));
            return true;
        }
    }
}
