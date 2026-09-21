using GameFramework.UI;
using GameFramework.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>
    /// Demonstrates: a popup that pauses gameplay while open (a no-op here since this sample's
    /// bootstrapper doesn't register <c>GameFlow.IGameFlowService</c> - <see cref="NavigationService"/>
    /// resolves it softly and simply skips the pause), and opening a nested popup on top of it.
    /// </summary>
    public sealed class SettingsPopup : UIPopup
    {
        [SerializeField] private Button _exitGameButton;
        [SerializeField] private Button _closeButton;

        private void OnEnable()
        {
            if (_exitGameButton != null) _exitGameButton.onClick.AddListener(OnExitGameClicked);
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            if (_exitGameButton != null) _exitGameButton.onClick.RemoveListener(OnExitGameClicked);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnExitGameClicked() =>
            Phase12DemoServices.Navigation?.OpenPopup(Phase12ScreenIds.ConfirmExit, new NavigationRequestOptions(pausesGameplay: true, pauseReason: "Settings"));

        // Uses INavigationService.CloseTopPopup, never this popup's own inherited UIPopup.Close() -
        // closing through the base UIScreen/UIPopup API directly would bypass NavigationService's
        // own popup-stack bookkeeping (and its pause-token release) and desync the two.
        private void OnCloseClicked() => Phase12DemoServices.Navigation?.CloseTopPopup();
    }
}
