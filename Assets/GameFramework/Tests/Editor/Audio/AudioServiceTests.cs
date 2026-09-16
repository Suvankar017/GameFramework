using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Audio.Tests
{
    public class AudioServiceTests
    {
        private SettingsService _settings;
        private FakeTimeService _time;
        private AudioService _audio;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            var storage = new InMemoryPersistenceStorage();
            var persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            var events = new EventService();
            _settings = new SettingsService();
            _time = new FakeTimeService();

            var registry = new ServiceRegistry();
            registry.Register<IPersistenceService>(persistence);
            registry.MarkInitialized(typeof(IPersistenceService));
            persistence.Initialize(registry);

            registry.Register<IEventService>(events);
            registry.MarkInitialized(typeof(IEventService));
            events.Initialize(registry);

            registry.Register<ISettingsService>(_settings);
            registry.MarkInitialized(typeof(ISettingsService));
            _settings.Initialize(registry);

            registry.Register<ITimeService>(_time);
            registry.MarkInitialized(typeof(ITimeService));
            _time.Initialize(registry);

            _audio = new AudioService(voicePoolSize: 2, random: new FakeRandomSource());
            _audio.Initialize(registry);

            _clip = AudioClip.Create("test", 100, 1, 44100, false);
            _clip.SetData(new float[100], 0);
        }

        [TearDown]
        public void TearDown()
        {
            _audio.Shutdown();
            Object.DestroyImmediate(_clip);
        }

        private AudioCueAsset BuildCue(int maxConcurrent = 0, float minRetrigger = 0f)
        {
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            cue.Clips.Add(_clip);
            cue.MaxConcurrentInstances = maxConcurrent;
            cue.MinRetriggerInterval = minRetrigger;
            return cue;
        }

        [Test]
        public void CategoryVolumes_DefaultToOne()
        {
            Assert.AreEqual(1f, _audio.MasterVolume, 0.0001f);
            Assert.AreEqual(1f, _audio.GetCategoryVolume(AudioCategory.Music), 0.0001f);
            Assert.AreEqual(1f, _audio.GetCategoryVolume(AudioCategory.Sfx), 0.0001f);
        }

        [Test]
        public void SetCategoryVolume_UpdatesGetCategoryVolume()
        {
            _audio.SetCategoryVolume(AudioCategory.Music, 0.4f);

            Assert.AreEqual(0.4f, _audio.GetCategoryVolume(AudioCategory.Music), 0.0001f);
        }

        [Test]
        public void SetCategoryVolume_ClampsAboveOne()
        {
            _audio.SetCategoryVolume(AudioCategory.Sfx, 5f);

            Assert.AreEqual(1f, _audio.GetCategoryVolume(AudioCategory.Sfx), 0.0001f);
        }

        [Test]
        public void Play_NullCue_ReturnsNullHandle()
        {
            IAudioHandle handle = _audio.Play(null);

            Assert.AreSame(NullAudioHandle.Instance, handle);
        }

        [Test]
        public void Play_CueWithNoClips_ReturnsNullHandle()
        {
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();

            IAudioHandle handle = _audio.Play(cue);

            Assert.AreSame(NullAudioHandle.Instance, handle);
        }

        [Test]
        public void Play_ValidCue_ReturnsRealHandle()
        {
            AudioCueAsset cue = BuildCue();

            IAudioHandle handle = _audio.Play(cue);

            Assert.AreNotSame(NullAudioHandle.Instance, handle);
        }

        [Test]
        public void Play_ExceedsMaxConcurrentInstances_ReturnsNullHandleForExtraPlays()
        {
            AudioCueAsset cue = BuildCue(maxConcurrent: 1);

            IAudioHandle first = _audio.Play(cue);
            IAudioHandle second = _audio.Play(cue);

            Assert.AreNotSame(NullAudioHandle.Instance, first);
            Assert.AreSame(NullAudioHandle.Instance, second);
        }

        [Test]
        public void Play_WithinMinRetriggerInterval_ReturnsNullHandle()
        {
            AudioCueAsset cue = BuildCue(minRetrigger: 1f);
            _time.UnscaledTime = 10f;

            _audio.Play(cue);
            IAudioHandle second = _audio.Play(cue); // same UnscaledTime - too soon

            Assert.AreSame(NullAudioHandle.Instance, second);
        }

        [Test]
        public void Play_AfterRetriggerIntervalElapses_Succeeds()
        {
            AudioCueAsset cue = BuildCue(minRetrigger: 1f);
            _time.UnscaledTime = 10f;
            _audio.Play(cue);

            _time.UnscaledTime = 12f;
            IAudioHandle second = _audio.Play(cue);

            Assert.AreNotSame(NullAudioHandle.Instance, second);
        }

        [Test]
        public void Play_VoicePoolExhausted_ReturnsNullHandle()
        {
            AudioCueAsset cueA = BuildCue();
            AudioCueAsset cueB = BuildCue();
            AudioCueAsset cueC = BuildCue();

            _audio.Play(cueA); // fills voice 1 of 2
            _audio.Play(cueB); // fills voice 2 of 2
            IAudioHandle third = _audio.Play(cueC); // pool exhausted

            Assert.AreSame(NullAudioHandle.Instance, third);
        }

        [Test]
        public void StopAll_ImmediatelyFreesVoices_AllowingReplay()
        {
            AudioCueAsset cue = BuildCue(maxConcurrent: 1);
            _audio.Play(cue);

            _audio.StopAll();
            IAudioHandle replay = _audio.Play(cue);

            Assert.AreNotSame(NullAudioHandle.Instance, replay);
        }

        [Test]
        public void Handle_Stop_MakesIsPlayingFalse()
        {
            AudioCueAsset cue = BuildCue();
            IAudioHandle handle = _audio.Play(cue);

            handle.Stop();

            Assert.IsFalse(handle.IsPlaying);
        }

        [Test]
        public void IsMuted_SuppressesEffectiveVolumeCalculation()
        {
            _audio.IsMuted = true;

            Assert.AreEqual(0f, GetEffective(_audio, AudioCategory.Sfx), 0.0001f);
        }

        private static float GetEffective(AudioService audio, AudioCategory category) =>
            audio.GetEffectiveCategoryVolume(category);
    }
}
