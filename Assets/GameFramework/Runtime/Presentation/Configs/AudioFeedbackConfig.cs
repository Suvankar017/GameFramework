using System;
using GameFramework.Audio;

namespace GameFramework.Presentation.Configs
{
    /// <summary>Requests <see cref="IAudioService.Play"/> for <see cref="Cue"/> - never touches an
    /// <c>AudioSource</c> directly, and never duplicates <see cref="AudioCueAsset"/>'s own
    /// randomization/limiting. <see cref="FeedbackRequest.Intensity"/> scales the returned handle's
    /// volume via <see cref="Audio.IAudioHandle.SetVolume"/>.</summary>
    [Serializable]
    public sealed class AudioFeedbackConfig
    {
        public bool Enabled;
        public AudioCueAsset Cue;
    }
}
