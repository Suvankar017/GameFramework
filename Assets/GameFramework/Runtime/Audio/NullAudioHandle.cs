namespace GameFramework.Audio
{
    /// <summary>No-op handle returned when <see cref="IAudioService.Play"/> declines to play a
    /// sound (invalid cue, voice pool exhausted, or cue limiting) — callers never need a null
    /// check on the returned handle.</summary>
    public sealed class NullAudioHandle : IAudioHandle
    {
        public static readonly NullAudioHandle Instance = new NullAudioHandle();

        private NullAudioHandle()
        {
        }

        public bool IsPlaying => false;
        public void Stop(float fadeOutSeconds = 0f)
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void SetVolume(float volume01)
        {
        }

        public void SetPitch(float pitch)
        {
        }
    }
}
