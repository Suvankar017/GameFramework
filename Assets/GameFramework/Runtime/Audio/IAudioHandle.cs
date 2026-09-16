namespace GameFramework.Audio
{
    /// <summary>A handle to one playing sound, returned by <see cref="IAudioService.Play"/>. Never
    /// exposes the underlying <see cref="UnityEngine.AudioSource"/> — callers control playback
    /// only through this narrow surface.</summary>
    public interface IAudioHandle
    {
        bool IsPlaying { get; }
        void Stop(float fadeOutSeconds = 0f);
        void Pause();
        void Resume();
        void SetVolume(float volume01);
        void SetPitch(float pitch);
    }
}
