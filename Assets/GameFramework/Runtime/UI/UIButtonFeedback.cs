using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    /// <summary>
    /// Opt-in audio/haptic feedback for a <see cref="Button"/> — both fields are optional so
    /// feedback is never added to a button unless a designer explicitly configures it here.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFeedback : MonoBehaviour
    {
        [SerializeField] private AudioCueAsset _clickSound;
        [SerializeField] private FeedbackPresetAsset _feedbackPreset;

        private Button _button;

        private void Awake() => _button = GetComponent<Button>();
        private void OnEnable() => _button.onClick.AddListener(OnClicked);
        private void OnDisable() => _button.onClick.RemoveListener(OnClicked);

        private void OnClicked()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                return;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;

            if (_clickSound != null && services.TryGet(out IAudioService audio))
            {
                audio.Play(_clickSound);
            }

            if (_feedbackPreset != null && services.TryGet(out IFeedbackService feedback))
            {
                feedback.TriggerPreset(_feedbackPreset);
            }
        }
    }
}
