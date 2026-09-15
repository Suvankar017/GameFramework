using GameFramework.Audio;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using UnityEngine;

namespace GameFramework.Feedback
{
    /// <summary>Default <see cref="IFeedbackService"/>. The haptics-enabled setting is a normal
    /// <see cref="ISettingsService"/> boolean ("Feedback.HapticsEnabled"), so it is persisted and
    /// editable the same way every other player setting is.</summary>
    public sealed class FeedbackService : IFeedbackService
    {
        private const string HapticsEnabledKey = "Feedback.HapticsEnabled";
        private const string LogCategory = "Feedback";

        private readonly IHapticProvider _hapticProvider;
        private ISettingsService _settings;
        private IAudioService _audio;
        private ILoggingService _log;

        public FeedbackService() : this(Application.isMobilePlatform ? new MobileHapticProvider() : new NoOpHapticProvider())
        {
        }

        internal FeedbackService(IHapticProvider hapticProvider)
        {
            _hapticProvider = Guard.NotNull(hapticProvider, nameof(hapticProvider));
        }

        public bool HapticsEnabled => _settings.Get<bool>(HapticsEnabledKey);

        public void Initialize(IServiceRegistry registry)
        {
            _settings = registry.Get<ISettingsService>();
            registry.TryGet(out _audio); // optional: only needed for presets that carry an audio cue
            registry.TryGet(out _log);

            _settings.Register(new SettingDefinition<bool>(HapticsEnabledKey, true, category: "Feedback"));
            _settings.Load(); // re-load: Settings.Initialize's own Load already ran before this
        }

        public void Shutdown()
        {
        }

        public void TriggerHaptic(HapticStrength strength)
        {
            if (!HapticsEnabled || !_hapticProvider.IsSupported)
            {
                return;
            }

            _hapticProvider.Trigger(strength);
        }

        public void TriggerPreset(FeedbackPresetAsset preset)
        {
            if (preset == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "TriggerPreset called with a null preset.");
                return;
            }

            if (preset.TriggerHaptic)
            {
                TriggerHaptic(preset.Haptic);
            }

            if (preset.AudioCue != null)
            {
                if (_audio != null)
                {
                    _audio.Play(preset.AudioCue);
                }
                else
                {
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"Preset '{preset.name}' has an audio cue but no IAudioService is registered.");
                }
            }
        }
    }
}
