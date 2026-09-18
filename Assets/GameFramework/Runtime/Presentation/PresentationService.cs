using System;
using System.Collections.Generic;
using GameFramework.Audio;
using GameFramework.Core.Validation;
using GameFramework.Feedback;
using GameFramework.Gameplay.Pooling;
using GameFramework.Performance.Profiling;
using GameFramework.Presentation.Configs;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using GameFramework.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Default <see cref="IPresentationService"/>.
    ///
    /// <para><b>Composable vs. exclusive channels.</b> Audio/Haptics/Visual/UI simply fire and
    /// forget - many instances coexist with no notion of "current." Camera shake is composable too,
    /// but in a different sense: multiple simultaneous shakes sum into one offset (see
    /// <see cref="CameraShakeState"/>). Screen and Time are exclusive: each has exactly one "current"
    /// effect, and a new request only interrupts it if the new <see cref="FeedbackDefinition.Priority"/>
    /// is greater than or equal to the current one's (see <see cref="ScreenEffectController"/>/
    /// <see cref="TimeFeedbackController"/>) - CLAUDE.md's Phase 10 brief, sections 20-21.</para>
    ///
    /// <para><b>Not duplicated here.</b> Audio playback (<see cref="IAudioService"/>), haptics
    /// (<see cref="IFeedbackService"/>), pooling (<see cref="Gameplay.Pooling.IPoolService"/>), UI
    /// layers (<see cref="IUIService"/>), and time-scale ownership (<see cref="ITimeService"/>) are
    /// all reused directly - this service only decides *when* to call them and combines the result.</para>
    /// </summary>
    public sealed class PresentationService : IPresentationService, IUpdatableService
    {
        private const string LogCategory = "Presentation";
        private const string EnabledKey = "Presentation.Enabled";
        private const string IntensityScaleKey = "Presentation.IntensityScale";
        private const string ChannelKeyPrefix = "Presentation.Channel.";

        private static readonly FeedbackChannel[] AllChannels =
        {
            FeedbackChannel.Audio, FeedbackChannel.Haptics, FeedbackChannel.Camera,
            FeedbackChannel.Visual, FeedbackChannel.Screen, FeedbackChannel.UI, FeedbackChannel.Time
        };

        private readonly Dictionary<FeedbackId, FeedbackDefinition> _definitions = new Dictionary<FeedbackId, FeedbackDefinition>();
        private readonly Dictionary<Type, Action> _mappingUnsubscribers = new Dictionary<Type, Action>();
        private readonly HashSet<string> _loggedMissingChannels = new HashSet<string>();
        private readonly ScreenEffectController _screenController = new ScreenEffectController();
        private readonly TimeFeedbackController _timeController = new TimeFeedbackController();

        private ISettingsService _settings;
        private IEventService _events;
        private ITimeService _time;
        private ITimerService _timers;
        private ILoggingService _log;
        private IAudioService _audio;
        private IFeedbackService _feedback;
        private IUIService _ui;
        private IPoolService _pool;

        private ICameraFeedbackDriver _cameraDriver;
        private GameObject _screenOverlayObject;
        private Image _screenOverlayImage;

        public void Initialize(IServiceRegistry registry)
        {
            _settings = registry.Get<ISettingsService>();
            _events = registry.Get<IEventService>();
            _time = registry.Get<ITimeService>();
            _timers = registry.Get<ITimerService>();
            registry.TryGet(out _log);
            registry.TryGet(out _audio);
            registry.TryGet(out _feedback);
            registry.TryGet(out _ui);
            registry.TryGet(out _pool);

            _settings.Register(new SettingDefinition<bool>(EnabledKey, true, category: "Presentation"));
            _settings.Register(new SettingDefinition<float>(IntensityScaleKey, 1f, v => v >= 0f && v <= 1f, category: "Presentation"));
            for (int i = 0; i < AllChannels.Length; i++)
            {
                _settings.Register(new SettingDefinition<bool>(ChannelKey(AllChannels[i]), true, category: "Presentation"));
            }

            _settings.Load(); // re-load: Settings.Initialize's own Load already ran before this
        }

        public void Shutdown()
        {
            foreach (Action unsubscribe in _mappingUnsubscribers.Values)
            {
                unsubscribe();
            }
            _mappingUnsubscribers.Clear();

            if (_timeController.IsActive)
            {
                _time.ResetTimeScale();
            }

            if (_screenOverlayObject != null)
            {
                Object.Destroy(_screenOverlayObject);
                _screenOverlayObject = null;
                _screenOverlayImage = null;
            }

            _cameraDriver = null;
        }

        public void Tick()
        {
            float unscaledDeltaTime = _time.UnscaledDeltaTime;

            if (_timeController.Tick(unscaledDeltaTime))
            {
                _time.ResetTimeScale();
            }

            if (_screenOverlayImage != null)
            {
                _screenController.Tick(unscaledDeltaTime);
                ApplyScreenOverlay();
            }
        }

        public void RegisterDefinition(FeedbackDefinition definition)
        {
            Guard.NotNull(definition, nameof(definition));

            FeedbackId id = definition.Id;
            if (!id.IsValid)
            {
                throw new ArgumentException("FeedbackDefinition has no Id assigned.", nameof(definition));
            }

            if (_definitions.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate feedback id '{id}'.");
            }

            _definitions.Add(id, definition);
        }

        public bool IsRegistered(FeedbackId id) => _definitions.ContainsKey(id);
        public FeedbackDefinition GetDefinition(FeedbackId id) => _definitions.TryGetValue(id, out FeedbackDefinition definition) ? definition : null;

        public PlayResult Play(FeedbackId id, Vector3? worldPosition = null, Object source = null, float intensity = 1f) =>
            PlayInternal(new FeedbackRequest(id, worldPosition, source, intensity));

        private PlayResult PlayInternal(FeedbackRequest request)
        {
            using (new ProfileScope(ProfilingCategory.Presentation))
            {
                if (!_definitions.TryGetValue(request.Id, out FeedbackDefinition definition))
                {
                    _events.Publish(new FeedbackPlayedEvent(request.Id, PlayResult.NotFound));
                    return PlayResult.NotFound;
                }

                if (!_settings.Get<bool>(EnabledKey))
                {
                    _events.Publish(new FeedbackPlayedEvent(request.Id, PlayResult.Suppressed));
                    return PlayResult.Suppressed;
                }

                float intensity = Mathf.Clamp01(request.Intensity * _settings.Get<float>(IntensityScaleKey));

                if (definition.Audio.Enabled && IsChannelEnabled(FeedbackChannel.Audio))
                {
                    ExecuteAudio(definition.Audio, request, intensity);
                }

                if (definition.Haptic.Enabled && IsChannelEnabled(FeedbackChannel.Haptics))
                {
                    ExecuteHaptic(definition.Haptic);
                }

                if (definition.Camera.Enabled && IsChannelEnabled(FeedbackChannel.Camera))
                {
                    ExecuteCamera(definition.Camera, intensity);
                }

                if (definition.Visual.Enabled && IsChannelEnabled(FeedbackChannel.Visual))
                {
                    ExecuteVisual(definition.Visual, request);
                }

                if (definition.Screen.Enabled && IsChannelEnabled(FeedbackChannel.Screen))
                {
                    ExecuteScreen(definition.Screen, intensity, definition.Priority);
                }

                if (definition.UI.Enabled && IsChannelEnabled(FeedbackChannel.UI))
                {
                    ExecuteUI(definition.UI, request.Id);
                }

                if (definition.Time.Enabled && IsChannelEnabled(FeedbackChannel.Time))
                {
                    ExecuteTime(definition.Time, definition.Priority);
                }

                _events.Publish(new FeedbackPlayedEvent(request.Id, PlayResult.Success));
                return PlayResult.Success;
            }
        }

        public void RegisterMapping<TEvent>(FeedbackId id, Func<TEvent, FeedbackRequest> requestFactory = null)
        {
            Type eventType = typeof(TEvent);
            if (_mappingUnsubscribers.ContainsKey(eventType))
            {
                throw new InvalidOperationException($"A feedback mapping for event type '{eventType.Name}' is already registered.");
            }

            void Handler(TEvent payload) => PlayInternal(requestFactory != null ? requestFactory(payload) : new FeedbackRequest(id));

            _events.Subscribe<TEvent>(Handler);
            _mappingUnsubscribers[eventType] = () => _events.Unsubscribe<TEvent>(Handler);
        }

        public void UnregisterMapping<TEvent>()
        {
            Type eventType = typeof(TEvent);
            if (_mappingUnsubscribers.TryGetValue(eventType, out Action unsubscribe))
            {
                unsubscribe();
                _mappingUnsubscribers.Remove(eventType);
            }
        }

        public void RegisterCameraDriver(ICameraFeedbackDriver driver) => _cameraDriver = driver;

        public void UnregisterCameraDriver(ICameraFeedbackDriver driver)
        {
            if (_cameraDriver == driver)
            {
                _cameraDriver = null;
            }
        }

        private void ExecuteAudio(AudioFeedbackConfig config, FeedbackRequest request, float intensity)
        {
            if (_audio == null || config.Cue == null)
            {
                LogMissingServiceOnce("Audio");
                return;
            }

            IAudioHandle handle = _audio.Play(config.Cue, request.WorldPosition);

            // Only override volume below full intensity - at 1.0 the cue's own authored/randomized
            // volume (AudioCueAsset.MinVolume/MaxVolume) is left exactly as IAudioService set it,
            // since SetVolume is an absolute override, not a multiplier, and there is no "playing at
            // full strength" volume to read back and scale from.
            if (intensity < 1f)
            {
                handle.SetVolume(intensity);
            }
        }

        private void ExecuteHaptic(HapticFeedbackConfig config)
        {
            if (_feedback == null)
            {
                LogMissingServiceOnce("Haptics");
                return;
            }

            _feedback.TriggerHaptic(config.Strength);
        }

        private void ExecuteCamera(CameraFeedbackConfig config, float intensity)
        {
            if (_cameraDriver == null)
            {
                LogMissingServiceOnce("Camera");
                return;
            }

            Vector3 axisMask = config.ConstrainToXY ? new Vector3(1f, 1f, 0f) : Vector3.one;
            _cameraDriver.RequestShake(new CameraShakeRequest(config.Amplitude * intensity, config.Frequency, config.Duration, config.Falloff, axisMask));
        }

        private void ExecuteVisual(VisualEffectFeedbackConfig config, FeedbackRequest request)
        {
            if (config.EffectPrefab == null)
            {
                return; // authoring gap, already flagged by FeedbackDefinition.OnValidate
            }

            Vector3 position = request.WorldPosition ?? Vector3.zero;

            if (config.UsePooling && _pool != null)
            {
                string key = "Presentation.Effect." + config.EffectPrefab.GetInstanceID();
                GameObjectPool pool = _pool.GetOrCreate(key, config.EffectPrefab);
                GameObject instance = pool.Get(position, Quaternion.identity);
                _timers.StartOneShot(config.Lifetime, () => pool.Release(instance), owner: instance);
            }
            else
            {
                GameObject instance = Object.Instantiate(config.EffectPrefab, position, Quaternion.identity);
                Object.Destroy(instance, config.Lifetime);
            }
        }

        private void ExecuteScreen(ScreenEffectFeedbackConfig config, float intensity, FeedbackPriority priority)
        {
            EnsureScreenOverlay();
            if (_screenOverlayImage == null)
            {
                LogMissingServiceOnce("Screen");
                return;
            }

            if (_screenController.TryPlay(config, intensity, priority))
            {
                _screenOverlayObject.SetActive(true);
            }
        }

        private void ExecuteUI(UIFeedbackConfig config, FeedbackId id) => _events.Publish(new UIFeedbackRequestedEvent(id, config.Tag));

        private void ExecuteTime(TimeFeedbackConfig config, FeedbackPriority priority)
        {
            if (_timeController.TryPlay(config, priority))
            {
                _time.SetTimeScale(config.TimeScale);
            }
        }

        private void EnsureScreenOverlay()
        {
            if (_screenOverlayImage != null || _ui == null)
            {
                return;
            }

            var go = new GameObject("PresentationScreenOverlay", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_ui.GetLayerRoot(UILayer.Overlay), worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _screenOverlayImage = go.GetComponent<Image>();
            _screenOverlayImage.raycastTarget = false;
            _screenOverlayImage.color = new Color(0f, 0f, 0f, 0f);

            _screenOverlayObject = go;
            _screenOverlayObject.SetActive(false);
        }

        private void ApplyScreenOverlay()
        {
            Color color = _screenController.Color;
            _screenOverlayImage.color = new Color(color.r, color.g, color.b, _screenController.CurrentAlpha);

            if (!_screenController.IsActive)
            {
                _screenOverlayObject.SetActive(false);
            }
        }

        private bool IsChannelEnabled(FeedbackChannel channel) => _settings.Get<bool>(ChannelKey(channel));

        private static string ChannelKey(FeedbackChannel channel) => ChannelKeyPrefix + channel;

        private void LogMissingServiceOnce(string channelName)
        {
            if (_loggedMissingChannels.Add(channelName))
            {
                _log?.Log(LogLevel.Warning, LogCategory,
                    $"'{channelName}' feedback was requested but its backing service/driver is not available; ignoring (logged once).");
            }
        }
    }
}
