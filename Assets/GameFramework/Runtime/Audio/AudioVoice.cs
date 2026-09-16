using GameFramework.Core.Extensions;
using UnityEngine;

namespace GameFramework.Audio
{
    /// <summary>
    /// One pooled playback slot. Owned exclusively by <see cref="AudioService"/> — game code only
    /// ever sees this behind the narrow <see cref="IAudioHandle"/> interface returned from
    /// <see cref="IAudioService.Play"/>. Volume fades run on unscaled time so UI/menu sounds keep
    /// fading correctly even while gameplay is paused (<c>ITimeService.Pause</c> zeroes scaled
    /// time only).
    /// </summary>
    internal sealed class AudioVoice : MonoBehaviour, IAudioHandle
    {
        internal AudioCategory Category { get; private set; }
        internal AudioCueAsset SourceCue { get; private set; }
        internal bool IsFree { get; private set; } = true;

        /// <summary>True for a general pooled voice (returned to <see cref="AudioService"/>'s free
        /// queue on reclaim); false for a dedicated music crossfade slot, which is never pooled.</summary>
        internal bool IsPooled { get; set; }

        private AudioSource _source;
        private AudioService _owner;
        private float _cueVolume;
        private float _fadeInDuration;
        private float _fadeOutDuration;
        private float _fadeTimer;
        private float _fadeOutStartVolume;
        private bool _isFadingOut;
        private bool _isPaused;

        // Resolved lazily rather than in Awake(): Awake is not guaranteed to have run yet when a
        // GameObject is created and used within the same synchronous call (e.g. AudioService.Initialize
        // creating and immediately playing a voice from an EditMode test, with no engine tick in
        // between) — only reliable in Play Mode's own frame loop, not the Editor's idle loop.
        private AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    _source = gameObject.GetOrAddComponent<AudioSource>();
                    _source.playOnAwake = false;
                }

                return _source;
            }
        }

        internal void Play(
            AudioService owner, AudioClip clip, AudioCategory category, AudioCueAsset sourceCue,
            float cueVolume, float pitch, bool loop, float fadeIn, float fadeOut, Vector3? worldPosition)
        {
            _owner = owner;
            Category = category;
            SourceCue = sourceCue;
            _cueVolume = cueVolume;
            _fadeInDuration = Mathf.Max(0f, fadeIn);
            _fadeOutDuration = Mathf.Max(0f, fadeOut);
            _fadeTimer = 0f;
            _isFadingOut = false;
            _isPaused = false;
            IsFree = false;

            Source.clip = clip;
            Source.loop = loop;
            Source.pitch = pitch;
            Source.spatialBlend = worldPosition.HasValue ? 1f : 0f;
            if (worldPosition.HasValue)
            {
                transform.position = worldPosition.Value;
            }

            Source.volume = _fadeInDuration > 0f ? 0f : ComputeTargetVolume();

            if (Application.isPlaying)
            {
                // AudioSource.Play() on a real clip is only reliably supported while the runtime
                // audio engine is actually running (Play Mode or a build) — outside that (e.g. a
                // service exercised directly from an EditMode test) it can throw inside Unity's
                // own native binding. Pool/limiting bookkeeping above still applies either way, so
                // logic under test doesn't depend on real playback having started.
                Source.Play();
            }
        }

        /// <summary>Advances fades and reclaims the voice once playback (and any fade-out) has
        /// finished. Called once per frame by <see cref="AudioService.Tick"/> for every active
        /// voice — never by the voice itself.</summary>
        internal void Tick(float unscaledDeltaTime)
        {
            if (IsFree || _isPaused)
            {
                return;
            }

            if (_isFadingOut)
            {
                _fadeTimer += unscaledDeltaTime;
                float t = _fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(_fadeTimer / _fadeOutDuration);
                Source.volume = Mathf.Lerp(_fadeOutStartVolume, 0f, t);

                if (t >= 1f)
                {
                    Source.Stop();
                    Reclaim();
                }

                return;
            }

            float target = ComputeTargetVolume();
            if (_fadeInDuration > 0f && _fadeTimer < _fadeInDuration)
            {
                _fadeTimer += unscaledDeltaTime;
                Source.volume = Mathf.Lerp(0f, target, Mathf.Clamp01(_fadeTimer / _fadeInDuration));
            }
            else
            {
                Source.volume = target;
            }

            if (!Source.loop && !Source.isPlaying)
            {
                Reclaim();
            }
        }

        private float ComputeTargetVolume() =>
            _cueVolume * (_owner != null ? _owner.GetEffectiveCategoryVolume(Category) : 1f);

        private void Reclaim()
        {
            AudioCueAsset cue = SourceCue;
            AudioService owner = _owner;

            IsFree = true;
            SourceCue = null;
            _owner = null;
            Source.clip = null;

            // Notified synchronously (not left for the next Tick) so that a caller doing
            // handle.Stop() immediately followed by Play(sameCue) within the same frame sees this
            // voice's capacity freed up right away, rather than being incorrectly limited by
            // AudioCueAsset.MaxConcurrentInstances until the next Tick reconciles it.
            owner?.HandleVoiceReclaimed(this, cue);
        }

        public bool IsPlaying => !IsFree && Source.isPlaying;

        public void Stop(float fadeOutSeconds = 0f)
        {
            if (IsFree)
            {
                return;
            }

            if (fadeOutSeconds <= 0f)
            {
                Source.Stop();
                Reclaim();
                return;
            }

            _isFadingOut = true;
            _fadeOutStartVolume = Source.volume;
            _fadeOutDuration = fadeOutSeconds;
            _fadeTimer = 0f;
        }

        public void Pause()
        {
            if (!IsFree)
            {
                Source.Pause();
                _isPaused = true;
            }
        }

        public void Resume()
        {
            if (!IsFree)
            {
                Source.UnPause();
                _isPaused = false;
            }
        }

        public void SetVolume(float volume01) => _cueVolume = Mathf.Clamp01(volume01);

        public void SetPitch(float pitch)
        {
            if (!IsFree)
            {
                Source.pitch = pitch;
            }
        }
    }
}
