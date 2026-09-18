using System.Collections.Generic;
using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Timers;
using GameFramework.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Presentation.Tests
{
    public class PresentationServiceTests
    {
        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private EventService _events;
        private SettingsService _settings;
        private TimerService _timers;
        private PresentationService _presentation;
        private FakeUIService _ui;
        private readonly List<Object> _createdAssets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _time, out _events, out _settings, out _timers);
            _presentation = new PresentationService();
        }

        [TearDown]
        public void TearDown()
        {
            _presentation.Shutdown();
            _ui?.DestroyAll();

            foreach (Object asset in _createdAssets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }
            _createdAssets.Clear();
        }

        private FeedbackDefinition Track(FeedbackDefinition definition)
        {
            _createdAssets.Add(definition);
            return definition;
        }

        private FakeAudioService RegisterAudio()
        {
            var audio = new FakeAudioService();
            _registry.Register<IAudioService>(audio);
            _registry.MarkInitialized(typeof(IAudioService));
            return audio;
        }

        private FakeFeedbackService RegisterFeedback()
        {
            var feedback = new FakeFeedbackService();
            _registry.Register<IFeedbackService>(feedback);
            _registry.MarkInitialized(typeof(IFeedbackService));
            return feedback;
        }

        private FakeUIService RegisterUI()
        {
            _ui = new FakeUIService();
            _registry.Register<IUIService>(_ui);
            _registry.MarkInitialized(typeof(IUIService));
            return _ui;
        }

        [Test]
        public void Play_UnregisteredId_ReturnsNotFound()
        {
            _presentation.Initialize(_registry);

            PlayResult result = _presentation.Play(new FeedbackId("Nope"));

            Assert.AreEqual(PlayResult.NotFound, result);
        }

        [Test]
        public void RegisterDefinition_DuplicateId_Throws()
        {
            _presentation.Initialize(_registry);
            _presentation.RegisterDefinition(Track(TestDefinitions.Create("A")));

            Assert.Throws<System.InvalidOperationException>(() => _presentation.RegisterDefinition(Track(TestDefinitions.Create("A"))));
        }

        [Test]
        public void RegisterDefinition_NoId_Throws()
        {
            _presentation.Initialize(_registry);

            Assert.Throws<System.ArgumentException>(() => _presentation.RegisterDefinition(Track(TestDefinitions.Create(null))));
        }

        [Test]
        public void Play_MasterDisabled_ReturnsSuppressed_AndSkipsEveryChannel()
        {
            FakeAudioService audio = RegisterAudio();
            _presentation.Initialize(_registry);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(definition.Audio.Cue);
            _presentation.RegisterDefinition(definition);
            _settings.Set("Presentation.Enabled", false);

            PlayResult result = _presentation.Play(new FeedbackId("A"));

            Assert.AreEqual(PlayResult.Suppressed, result);
            Assert.AreEqual(0, audio.PlayedCues.Count);
        }

        [Test]
        public void Play_Audio_CallsAudioServicePlay_WithCueAndPosition()
        {
            FakeAudioService audio = RegisterAudio();
            _presentation.Initialize(_registry);
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"), worldPosition: new Vector3(1f, 2f, 3f));

            Assert.AreEqual(1, audio.PlayedCues.Count);
            Assert.AreSame(cue, audio.PlayedCues[0].Cue);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), audio.PlayedCues[0].Position);
        }

        [Test]
        public void Play_Audio_FullIntensity_DoesNotOverrideVolume()
        {
            FakeAudioService audio = RegisterAudio();
            _presentation.Initialize(_registry);
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"), intensity: 1f);

            Assert.IsNull(audio.LastHandle.LastSetVolume);
        }

        [Test]
        public void Play_Audio_ReducedIntensity_SetsVolume()
        {
            FakeAudioService audio = RegisterAudio();
            _presentation.Initialize(_registry);
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"), intensity: 0.4f);

            Assert.AreEqual(0.4f, audio.LastHandle.LastSetVolume.Value, 0.001f);
        }

        [Test]
        public void Play_Audio_NoAudioServiceRegistered_DoesNotThrow()
        {
            _presentation.Initialize(_registry); // Audio never registered on the registry
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            _presentation.RegisterDefinition(definition);

            Assert.DoesNotThrow(() => _presentation.Play(new FeedbackId("A")));
        }

        [Test]
        public void Play_Haptic_TriggersConfiguredStrength()
        {
            FakeFeedbackService feedback = RegisterFeedback();
            _presentation.Initialize(_registry);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Haptic.Enabled = true;
            definition.Haptic.Strength = HapticStrength.Heavy;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"));

            Assert.AreEqual(new[] { HapticStrength.Heavy }, feedback.TriggeredHaptics.ToArray());
        }

        [Test]
        public void Play_Camera_ForwardsShakeToRegisteredDriver()
        {
            _presentation.Initialize(_registry);
            var driver = new FakeCameraFeedbackDriver();
            _presentation.RegisterCameraDriver(driver);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Camera.Enabled = true;
            definition.Camera.Amplitude = 0.5f;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"), intensity: 0.5f);

            Assert.AreEqual(1, driver.Requests.Count);
            Assert.AreEqual(0.25f, driver.Requests[0].Amplitude, 0.001f); // Amplitude * intensity
        }

        [Test]
        public void Play_Camera_NoDriverRegistered_DoesNotThrow()
        {
            _presentation.Initialize(_registry);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Camera.Enabled = true;
            _presentation.RegisterDefinition(definition);

            Assert.DoesNotThrow(() => _presentation.Play(new FeedbackId("A")));
        }

        [Test]
        public void RegisterCameraDriver_UnregisterWithDifferentDriver_DoesNotClearCurrent()
        {
            _presentation.Initialize(_registry);
            var driverA = new FakeCameraFeedbackDriver();
            var driverB = new FakeCameraFeedbackDriver();
            _presentation.RegisterCameraDriver(driverA);

            _presentation.UnregisterCameraDriver(driverB); // stale/unrelated driver

            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Camera.Enabled = true;
            _presentation.RegisterDefinition(definition);
            _presentation.Play(new FeedbackId("A"));

            Assert.AreEqual(1, driverA.Requests.Count);
            Assert.AreEqual(0, driverB.Requests.Count);
        }

        [Test]
        public void Play_UI_PublishesUIFeedbackRequestedEvent()
        {
            _presentation.Initialize(_registry);
            UIFeedbackRequestedEvent? received = null;
            _events.Subscribe<UIFeedbackRequestedEvent>(e => received = e);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.UI.Enabled = true;
            definition.UI.Tag = "Reward";
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"));

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("Reward", received.Value.Tag);
        }

        [Test]
        public void Play_Time_SetsTimeScale_AndResetsAfterDurationElapses()
        {
            _presentation.Initialize(_registry);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Time.Enabled = true;
            definition.Time.TimeScale = 0.05f;
            definition.Time.Duration = 0.2f;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"));
            Assert.AreEqual(0.05f, _time.TimeScale);

            _time.UnscaledDeltaTime = 0.1f;
            _presentation.Tick();
            Assert.AreEqual(0.05f, _time.TimeScale, "Must still be active after only half the duration.");

            _presentation.Tick();
            Assert.AreEqual(1f, _time.TimeScale, "Must reset once the duration has fully elapsed.");
        }

        [Test]
        public void Play_ChannelDisabledViaSettings_SkipsOnlyThatChannel()
        {
            FakeAudioService audio = RegisterAudio();
            FakeFeedbackService feedback = RegisterFeedback();
            _presentation.Initialize(_registry);
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            definition.Haptic.Enabled = true;
            _presentation.RegisterDefinition(definition);
            _settings.Set("Presentation.Channel.Audio", false);

            _presentation.Play(new FeedbackId("A"));

            Assert.AreEqual(0, audio.PlayedCues.Count);
            Assert.AreEqual(1, feedback.TriggeredHaptics.Count);
        }

        [Test]
        public void Play_PublishesFeedbackPlayedEvent_WithResult()
        {
            _presentation.Initialize(_registry);
            FeedbackPlayedEvent? received = null;
            _events.Subscribe<FeedbackPlayedEvent>(e => received = e);
            _presentation.RegisterDefinition(Track(TestDefinitions.Create("A")));

            _presentation.Play(new FeedbackId("A"));

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(new FeedbackId("A"), received.Value.Id);
            Assert.AreEqual(PlayResult.Success, received.Value.Result);
        }

        [Test]
        public void IntensityScaleSetting_ScalesEveryRequestsIntensity()
        {
            FakeAudioService audio = RegisterAudio();
            _presentation.Initialize(_registry);
            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            _createdAssets.Add(cue);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Audio.Enabled = true;
            definition.Audio.Cue = cue;
            _presentation.RegisterDefinition(definition);
            _settings.Set("Presentation.IntensityScale", 0.5f);

            _presentation.Play(new FeedbackId("A"), intensity: 1f);

            Assert.AreEqual(0.5f, audio.LastHandle.LastSetVolume.Value, 0.001f);
        }

        [Test]
        public void RegisterMapping_EventPublished_AutomaticallyPlaysFeedback()
        {
            _presentation.Initialize(_registry);
            FeedbackPlayedEvent? received = null;
            _events.Subscribe<FeedbackPlayedEvent>(e => received = e);
            _presentation.RegisterDefinition(Track(TestDefinitions.Create("A")));
            _presentation.RegisterMapping<TestSignal>(new FeedbackId("A"));

            _events.Publish(new TestSignal());

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(new FeedbackId("A"), received.Value.Id);
        }

        [Test]
        public void RegisterMapping_DuplicateEventType_Throws()
        {
            _presentation.Initialize(_registry);
            _presentation.RegisterMapping<TestSignal>(new FeedbackId("A"));

            Assert.Throws<System.InvalidOperationException>(() => _presentation.RegisterMapping<TestSignal>(new FeedbackId("B")));
        }

        [Test]
        public void UnregisterMapping_StopsFurtherAutomaticPlay()
        {
            _presentation.Initialize(_registry);
            int playCount = 0;
            _events.Subscribe<FeedbackPlayedEvent>(e => playCount++);
            _presentation.RegisterDefinition(Track(TestDefinitions.Create("A")));
            _presentation.RegisterMapping<TestSignal>(new FeedbackId("A"));

            _presentation.UnregisterMapping<TestSignal>();
            _events.Publish(new TestSignal());

            Assert.AreEqual(0, playCount);
        }

        [Test]
        public void Shutdown_ActiveTimeEffect_ResetsTimeScale()
        {
            _presentation.Initialize(_registry);
            FeedbackDefinition definition = Track(TestDefinitions.Create("A"));
            definition.Time.Enabled = true;
            definition.Time.TimeScale = 0.1f;
            definition.Time.Duration = 10f;
            _presentation.RegisterDefinition(definition);
            _presentation.Play(new FeedbackId("A"));
            Assert.AreEqual(0.1f, _time.TimeScale);

            _presentation.Shutdown();

            Assert.AreEqual(1f, _time.TimeScale);
        }

        // Visual-effect spawning (both the pooled and plain-Instantiate paths) is covered in
        // GameFramework.Presentation.Tests.Runtime (PlayMode) instead of here: the non-pooled
        // fallback calls UnityEngine.Object.Destroy(instance, lifetime), which Unity does not allow
        // outside Play Mode ("Destroy may not be called from edit mode!") - a real Editor
        // restriction, not a framework bug, exactly the kind of case CLAUDE.md's Phase 10 brief
        // (section 38) already calls out PlayMode tests for.

        private readonly struct TestSignal
        {
        }
    }
}
