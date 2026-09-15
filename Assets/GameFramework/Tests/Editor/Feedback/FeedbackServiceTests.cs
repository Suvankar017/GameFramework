using GameFramework.Audio;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Feedback.Tests
{
    public class FeedbackServiceTests
    {
        private SettingsService _settings;
        private FakeHapticProvider _haptics;
        private FakeAudioService _audio;
        private FeedbackService _feedback;

        [SetUp]
        public void SetUp()
        {
            var storage = new InMemoryPersistenceStorage();
            var persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            var events = new EventService();
            _settings = new SettingsService();
            _audio = new FakeAudioService();

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

            registry.Register<IAudioService>(_audio);
            registry.MarkInitialized(typeof(IAudioService));
            _audio.Initialize(registry);

            _haptics = new FakeHapticProvider();
            _feedback = new FeedbackService(_haptics);
            _feedback.Initialize(registry);
        }

        [Test]
        public void HapticsEnabled_DefaultsToTrue()
        {
            Assert.IsTrue(_feedback.HapticsEnabled);
        }

        [Test]
        public void TriggerHaptic_WhenEnabledAndSupported_CallsProvider()
        {
            _feedback.TriggerHaptic(HapticStrength.Heavy);

            Assert.AreEqual(1, _haptics.TriggerCount);
            Assert.AreEqual(HapticStrength.Heavy, _haptics.LastStrength);
        }

        [Test]
        public void TriggerHaptic_WhenSettingDisabled_DoesNotCallProvider()
        {
            _settings.Set("Feedback.HapticsEnabled", false);

            _feedback.TriggerHaptic(HapticStrength.Light);

            Assert.AreEqual(0, _haptics.TriggerCount);
        }

        [Test]
        public void TriggerHaptic_WhenProviderUnsupported_DoesNotCallProvider()
        {
            _haptics.IsSupported = false;

            _feedback.TriggerHaptic(HapticStrength.Light);

            Assert.AreEqual(0, _haptics.TriggerCount);
        }

        [Test]
        public void TriggerPreset_WithHapticEnabled_TriggersHaptic()
        {
            var preset = ScriptableObject.CreateInstance<FeedbackPresetAsset>();
            preset.TriggerHaptic = true;
            preset.Haptic = HapticStrength.Success;

            _feedback.TriggerPreset(preset);

            Assert.AreEqual(1, _haptics.TriggerCount);
            Assert.AreEqual(HapticStrength.Success, _haptics.LastStrength);
        }

        [Test]
        public void TriggerPreset_WithHapticDisabled_DoesNotTriggerHaptic()
        {
            var preset = ScriptableObject.CreateInstance<FeedbackPresetAsset>();
            preset.TriggerHaptic = false;

            _feedback.TriggerPreset(preset);

            Assert.AreEqual(0, _haptics.TriggerCount);
        }

        [Test]
        public void TriggerPreset_WithAudioCue_PlaysThroughAudioService()
        {
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            var preset = ScriptableObject.CreateInstance<FeedbackPresetAsset>();
            preset.TriggerHaptic = false;
            preset.AudioCue = cue;

            _feedback.TriggerPreset(preset);

            Assert.AreEqual(1, _audio.PlayCount);
            Assert.AreEqual(cue, _audio.LastPlayedCue);
        }

        [Test]
        public void TriggerPreset_WithoutAudioCue_DoesNotPlayAudio()
        {
            var preset = ScriptableObject.CreateInstance<FeedbackPresetAsset>();

            _feedback.TriggerPreset(preset);

            Assert.AreEqual(0, _audio.PlayCount);
        }

        [Test]
        public void TriggerPreset_NullPreset_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _feedback.TriggerPreset(null));
        }

        [Test]
        public void TriggerPreset_AudioCueWithNoAudioServiceRegistered_DoesNotThrow()
        {
            var storage = new InMemoryPersistenceStorage();
            var persistence = new PersistenceService(storage, new JsonPersistenceSerializer());
            var events = new EventService();
            var settings = new SettingsService();

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

            var feedbackWithoutAudio = new FeedbackService(new FakeHapticProvider());
            feedbackWithoutAudio.Initialize(registry);

            var preset = ScriptableObject.CreateInstance<FeedbackPresetAsset>();
            preset.TriggerHaptic = false;
            preset.AudioCue = ScriptableObject.CreateInstance<AudioCueAsset>();

            Assert.DoesNotThrow(() => feedbackWithoutAudio.TriggerPreset(preset));
        }
    }
}
