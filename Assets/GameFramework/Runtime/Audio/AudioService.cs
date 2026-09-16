using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Audio
{
    /// <summary>
    /// Default <see cref="IAudioService"/>. Owns a fixed-size pool of <see cref="AudioVoice"/>
    /// instances under its own <c>DontDestroyOnLoad</c> root (created here, not authored in a
    /// scene, the same way <c>GameBootstrapper</c> owns its own root) plus two dedicated slots for
    /// music crossfade. Advances every active voice's fade envelope once per frame via
    /// <see cref="IUpdatableService.Tick"/>.
    /// </summary>
    public sealed class AudioService : IAudioService, IUpdatableService
    {
        private const string LogCategory = "Audio";
        private const string MasterVolumeKey = "Audio.MasterVolume";
        private const string MusicVolumeKey = "Audio.MusicVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";
        private const string UiVolumeKey = "Audio.UiVolume";
        private const string VoiceVolumeKey = "Audio.VoiceVolume";

        private readonly int _voicePoolSize;
        private readonly IRandomSource _random;

        private readonly Queue<AudioVoice> _freeVoices = new Queue<AudioVoice>();
        private readonly List<AudioVoice> _activeVoices = new List<AudioVoice>();
        private readonly Dictionary<AudioCueAsset, int> _activeCountByCue = new Dictionary<AudioCueAsset, int>();
        private readonly Dictionary<AudioCueAsset, float> _lastPlayedTimeByCue = new Dictionary<AudioCueAsset, float>();

        private ISettingsService _settings;
        private ITimeService _time;
        private ILoggingService _log;

        private GameObject _root;
        private AudioVoice _musicSlotA;
        private AudioVoice _musicSlotB;
        private AudioVoice _activeMusicSlot;
        private AudioApplicationLifecycleHook _lifecycleHook;

        /// <summary>Whether an application pause (e.g. the app going to background on mobile)
        /// pauses every active voice, resuming them on focus regain. Defaults to true.</summary>
        public bool PauseOnApplicationPause { get; set; } = true;

        public AudioService(int voicePoolSize = 16) : this(voicePoolSize, new UnityRandomSource())
        {
        }

        internal AudioService(int voicePoolSize, IRandomSource random)
        {
            _voicePoolSize = Mathf.Max(1, voicePoolSize);
            _random = Guard.NotNull(random, nameof(random));
        }

        public void Initialize(IServiceRegistry registry)
        {
            _settings = registry.Get<ISettingsService>();
            _time = registry.Get<ITimeService>();
            registry.TryGet(out _log);

            RegisterVolumeSetting(MasterVolumeKey);
            RegisterVolumeSetting(MusicVolumeKey);
            RegisterVolumeSetting(SfxVolumeKey);
            RegisterVolumeSetting(UiVolumeKey);
            RegisterVolumeSetting(VoiceVolumeKey);
            _settings.Load(); // re-load: Settings.Initialize's own Load already ran before this

            _root = new GameObject("AudioService (GameFramework)");
            if (Application.isPlaying)
            {
                // DontDestroyOnLoad throws outside Play Mode (e.g. a service constructed directly
                // from an EditMode test) — the root simply isn't scene-persistent in that context,
                // which is fine since nothing unloads it there either.
                Object.DontDestroyOnLoad(_root);
            }

            for (int i = 0; i < _voicePoolSize; i++)
            {
                AudioVoice voice = CreateVoice($"Voice_{i}");
                voice.IsPooled = true;
                _freeVoices.Enqueue(voice);
            }

            _musicSlotA = CreateVoice("MusicSlotA");
            _musicSlotB = CreateVoice("MusicSlotB");

            _lifecycleHook = _root.AddComponent<AudioApplicationLifecycleHook>();
            _lifecycleHook.ApplicationPauseChanged += OnApplicationPauseChanged;
        }

        public void Shutdown()
        {
            StopAll();
            if (_lifecycleHook != null)
            {
                _lifecycleHook.ApplicationPauseChanged -= OnApplicationPauseChanged;
            }

            if (_root != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_root);
                }
                else
                {
                    Object.DestroyImmediate(_root);
                }
            }

            _root = null;
            _freeVoices.Clear();
            _activeVoices.Clear();
            _activeCountByCue.Clear();
            _lastPlayedTimeByCue.Clear();
        }

        public void Tick()
        {
            float deltaTime = _time.UnscaledDeltaTime;

            // Iterated backward: a voice finishing this frame reclaims itself synchronously (see
            // HandleVoiceReclaimed), which removes it from _activeVoices mid-loop.
            for (int i = _activeVoices.Count - 1; i >= 0; i--)
            {
                _activeVoices[i].Tick(deltaTime);
            }

            _musicSlotA.Tick(deltaTime);
            _musicSlotB.Tick(deltaTime);
        }

        /// <summary>Called by <see cref="AudioVoice.Stop"/>/<see cref="AudioVoice.Tick"/> the
        /// moment a voice actually finishes (fade-out complete, or non-looping clip ended) — never
        /// deferred to the next <see cref="Tick"/>, so limiting (<see cref="AudioCueAsset.MaxConcurrentInstances"/>)
        /// reflects capacity freed by a Stop() within the same frame it happened.</summary>
        internal void HandleVoiceReclaimed(AudioVoice voice, AudioCueAsset cue)
        {
            if (cue != null && _activeCountByCue.TryGetValue(cue, out int count))
            {
                _activeCountByCue[cue] = Mathf.Max(0, count - 1);
            }

            if (voice.IsPooled && _activeVoices.Remove(voice))
            {
                _freeVoices.Enqueue(voice);
            }
        }

        public IAudioHandle Play(AudioCueAsset cue, Vector3? worldPosition = null)
        {
            if (cue == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "Play called with a null cue.");
                return NullAudioHandle.Instance;
            }

            if (!AudioCueSampler.TrySelect(cue, _random, out AudioClip clip, out float volume, out float pitch))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Cue '{cue.name}' has no clips assigned.");
                return NullAudioHandle.Instance;
            }

            if (IsLimited(cue))
            {
                return NullAudioHandle.Instance;
            }

            if (_freeVoices.Count == 0)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "Voice pool exhausted; dropping playback request.");
                return NullAudioHandle.Instance;
            }

            AudioVoice voice = _freeVoices.Dequeue();
            voice.Play(this, clip, cue.Category, cue, volume, pitch, cue.Loop, cue.FadeInSeconds, cue.FadeOutSeconds, worldPosition);
            _activeVoices.Add(voice);

            _activeCountByCue[cue] = _activeCountByCue.TryGetValue(cue, out int count) ? count + 1 : 1;
            _lastPlayedTimeByCue[cue] = _time.UnscaledTime;

            return voice;
        }

        public void PlayMusic(AudioCueAsset cue, float crossfadeSeconds = 0f)
        {
            if (cue == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "PlayMusic called with a null cue.");
                return;
            }

            if (!AudioCueSampler.TrySelect(cue, _random, out AudioClip clip, out float volume, out float pitch))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Music cue '{cue.name}' has no clips assigned.");
                return;
            }

            AudioVoice previous = _activeMusicSlot;
            AudioVoice next = ReferenceEquals(previous, _musicSlotA) ? _musicSlotB : _musicSlotA;

            next.Play(this, clip, AudioCategory.Music, cue, volume, pitch, loop: true, crossfadeSeconds, crossfadeSeconds, worldPosition: null);
            previous?.Stop(crossfadeSeconds);
            _activeMusicSlot = next;
        }

        public void StopMusic(float fadeOutSeconds = 0f)
        {
            _activeMusicSlot?.Stop(fadeOutSeconds);
            _activeMusicSlot = null;
        }

        public void StopAll(AudioCategory? category = null)
        {
            for (int i = _activeVoices.Count - 1; i >= 0; i--)
            {
                if (category == null || _activeVoices[i].Category == category.Value)
                {
                    _activeVoices[i].Stop();
                }
            }

            if (category == null || category.Value == AudioCategory.Music)
            {
                StopMusic();
            }
        }

        public float MasterVolume
        {
            get => _settings.Get<float>(MasterVolumeKey);
            set => _settings.Set(MasterVolumeKey, Mathf.Clamp01(value));
        }

        public bool IsMuted { get; set; }

        public void SetCategoryVolume(AudioCategory category, float volume01) =>
            _settings.Set(VolumeKeyFor(category), Mathf.Clamp01(volume01));

        public float GetCategoryVolume(AudioCategory category) => _settings.Get<float>(VolumeKeyFor(category));

        internal float GetEffectiveCategoryVolume(AudioCategory category)
        {
            if (IsMuted)
            {
                return 0f;
            }

            return MasterVolume * GetCategoryVolume(category);
        }

        private static string VolumeKeyFor(AudioCategory category) => category switch
        {
            AudioCategory.Music => MusicVolumeKey,
            AudioCategory.Sfx => SfxVolumeKey,
            AudioCategory.Ui => UiVolumeKey,
            AudioCategory.Voice => VoiceVolumeKey,
            _ => SfxVolumeKey
        };

        private void RegisterVolumeSetting(string key) =>
            _settings.Register(new SettingDefinition<float>(key, 1f, v => v is >= 0f and <= 1f, category: "Audio"));

        private bool IsLimited(AudioCueAsset cue)
        {
            if (cue.MinRetriggerInterval > 0f &&
                _lastPlayedTimeByCue.TryGetValue(cue, out float lastTime) &&
                _time.UnscaledTime - lastTime < cue.MinRetriggerInterval)
            {
                return true;
            }

            if (cue.MaxConcurrentInstances > 0 &&
                _activeCountByCue.TryGetValue(cue, out int count) &&
                count >= cue.MaxConcurrentInstances)
            {
                return true;
            }

            return false;
        }

        private AudioVoice CreateVoice(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform);
            return go.AddComponent<AudioVoice>();
        }

        private void OnApplicationPauseChanged(bool isPaused)
        {
            if (!PauseOnApplicationPause)
            {
                return;
            }

            foreach (AudioVoice voice in _activeVoices)
            {
                if (isPaused) voice.Pause(); else voice.Resume();
            }

            if (_activeMusicSlot != null)
            {
                if (isPaused) _activeMusicSlot.Pause(); else _activeMusicSlot.Resume();
            }
        }
    }
}
