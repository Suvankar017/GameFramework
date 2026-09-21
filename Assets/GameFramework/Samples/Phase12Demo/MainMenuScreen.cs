using GameFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>
    /// Root of the sample's navigation stack (opened via <c>INavigationService.Reset</c> from the
    /// composition root, so back can never return past it). Demonstrates: pushing a screen
    /// (<c>Play</c>) and opening a popup (<c>Settings</c>) - both go through
    /// <c>INavigationService</c>, never <c>IUIService</c> directly.
    /// </summary>
    public sealed class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        private void OnEnable()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void OnPlayClicked() => Phase12DemoServices.Navigation?.Navigate(Phase12ScreenIds.CharacterSelection);
        private void OnSettingsClicked() => Phase12DemoServices.Navigation?.OpenPopup(Phase12ScreenIds.Settings);
    }
}
