using System.Collections;
using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Input;
using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.UI;
using UnityEngine;

namespace GameFramework.Samples.Phase3Demo
{
    /// <summary>
    /// Drives the Phase 3 demonstration scene: exercises Input, Localization, Audio, UI, and
    /// Feedback together, with no authored assets required (every asset used is built in code).
    /// Not part of the reusable framework — sample/demo content only, kept in its own assembly and
    /// scene, separate from any production game content.
    /// </summary>
    public sealed class Phase3DemoController : MonoBehaviour
    {
        private IInputService _input;
        private IAudioService _audio;
        private IFeedbackService _feedback;
        private AudioCueAsset _beepCue;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;

            var localization = services.Get<ILocalizationService>();
            localization.LoadConfig(BuildDemoLocalizationConfig());
            Debug.Log($"[Phase3Demo] Localization (en): \"{localization.GetString("Demo.Hello")}\"");
            localization.SetLanguage("fr");
            Debug.Log($"[Phase3Demo] Localization (fr): \"{localization.GetString("Demo.Hello")}\"");
            localization.SetLanguage("en");

            _input = services.Get<IInputService>();
            _input.RegisterActionMap(BuildDemoInputMap());

            _audio = services.Get<IAudioService>();
            _beepCue = BuildDemoBeepCue();

            _feedback = services.Get<IFeedbackService>();

            var ui = services.Get<IUIService>();
            ui.OpenScreen(Phase3DemoScreen.Create());

            Debug.Log("[Phase3Demo] Ready. Press Space (or click) to trigger Input + Audio + Feedback.");
        }

        private void Update()
        {
            if (_input == null)
            {
                return; // Start's coroutine hasn't reached Ready yet
            }

            if (_input.GetButtonDown("Jump"))
            {
                Debug.Log("[Phase3Demo] Jump action triggered - playing audio cue and requesting haptic.");
                _audio.Play(_beepCue);
                _feedback.TriggerHaptic(HapticStrength.Light);
            }
        }

        private static LocalizationConfigAsset BuildDemoLocalizationConfig()
        {
            var english = ScriptableObject.CreateInstance<LocalizationTableAsset>();
            english.LanguageCode = "en";
            english.DisplayName = "English";
            english.Entries.Add(new LocalizationEntry { Key = "Demo.Hello", Value = "Hello from Phase 3!" });

            var french = ScriptableObject.CreateInstance<LocalizationTableAsset>();
            french.LanguageCode = "fr";
            french.DisplayName = "Français";
            french.Entries.Add(new LocalizationEntry { Key = "Demo.Hello", Value = "Bonjour depuis la Phase 3 !" });

            var config = ScriptableObject.CreateInstance<LocalizationConfigAsset>();
            config.DefaultLanguageCode = "en";
            config.Tables.Add(english);
            config.Tables.Add(french);
            return config;
        }

        private static InputActionMapAsset BuildDemoInputMap()
        {
            var map = ScriptableObject.CreateInstance<InputActionMapAsset>();
            map.Actions.Add(new InputActionBindingDefinition
            {
                ActionName = "Jump",
                Type = InputActionType.Button,
                KeyboardKeys = new[] { KeyCode.Space },
                MouseButtons = new[] { 0 }
            });
            return map;
        }

        private static AudioCueAsset BuildDemoBeepCue()
        {
            const int sampleRate = 44100;
            var samples = new float[sampleRate / 10]; // 0.1s beep
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate) * 0.3f;
            }

            AudioClip clip = AudioClip.Create("Phase3DemoBeep", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);

            var cue = ScriptableObject.CreateInstance<AudioCueAsset>();
            cue.Category = AudioCategory.Sfx;
            cue.Clips.Add(clip);
            return cue;
        }
    }
}
