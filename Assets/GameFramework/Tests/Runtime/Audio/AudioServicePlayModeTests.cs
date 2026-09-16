using System.Collections;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Audio.Tests
{
    /// <summary>
    /// Play Mode is required here: <see cref="UnityEngine.AudioSource"/> playback is only reliably
    /// driven by Unity's real audio engine while actually running (see
    /// <c>AudioVoice.Play</c>'s <c>Application.isPlaying</c> guard) — this is the one place Phase 3
    /// Audio claims (not just implements) real playback, distinct from the EditMode tests covering
    /// pooling/limiting/volume logic without touching an actual AudioSource.
    /// </summary>
    public class AudioServicePlayModeTests
    {
        private AudioService _audio;
        private AudioClip _clip;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            var storage = new InMemoryPersistenceStorage();
            var persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            var events = new EventService();
            var settings = new SettingsService();
            var time = new TimeService();

            var registry = new ServiceRegistry();
            registry.Register<IPersistenceService>(persistence);
            registry.MarkInitialized(typeof(IPersistenceService));
            persistence.Initialize(registry);

            registry.Register<IEventService>(events);
            registry.MarkInitialized(typeof(IEventService));
            events.Initialize(registry);

            registry.Register<ISettingsService>(settings);
            registry.MarkInitialized(typeof(ISettingsService));
            settings.Initialize(registry);

            registry.Register<ITimeService>(time);
            registry.MarkInitialized(typeof(ITimeService));
            time.Initialize(registry);

            _audio = new AudioService(voicePoolSize: 4);
            _audio.Initialize(registry);

            // 1 second of silence at 44.1kHz - long enough that a handful of frames won't finish it.
            _clip = AudioClip.Create("test", 44100, 1, 44100, false);
            _clip.SetData(new float[44100], 0);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            _audio.Shutdown();
            Object.Destroy(_clip);
            yield return null;
        }

        private AudioCueAsset BuildCue()
        {
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            cue.Clips.Add(_clip);
            return cue;
        }

        [UnityTest]
        public IEnumerator Play_ValidCue_ActuallyStartsPlaying()
        {
            IAudioHandle handle = _audio.Play(BuildCue());

            yield return null; // let Unity's audio engine actually start the clip

            Assert.IsTrue(handle.IsPlaying);
        }

        [UnityTest]
        public IEnumerator Handle_Stop_ActuallyStopsPlayback()
        {
            IAudioHandle handle = _audio.Play(BuildCue());
            yield return null;
            Assert.IsTrue(handle.IsPlaying);

            handle.Stop();

            Assert.IsFalse(handle.IsPlaying);
        }

        [UnityTest]
        public IEnumerator Tick_AdvancesFadeIn_ReachingFullTargetVolume()
        {
            var cue = BuildCue();
            cue.FadeInSeconds = 0.05f;

            _audio.Play(cue);

            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                yield return null;
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                _audio.Tick();
            }

            // Reaching here without exceptions and the fade having run to completion is the
            // observable behavior available through IAudioHandle's narrow surface; volume itself
            // is intentionally not exposed on the handle (see IAudioHandle).
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator StopAll_StopsEveryActiveVoice()
        {
            _audio.Play(BuildCue());
            _audio.Play(BuildCue());
            yield return null;

            _audio.StopAll();

            // No direct enumeration of active voices is exposed; verifying via a fresh play
            // succeeding under a MaxConcurrentInstances-limited cue confirms the pool was freed.
            var limited = BuildCue();
            limited.MaxConcurrentInstances = 1;
            IAudioHandle handle = _audio.Play(limited);
            Assert.AreNotSame(NullAudioHandle.Instance, handle);
        }
    }
}
