using System.Collections;
using GameFramework.UI;
using GameFramework.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>
    /// Demonstrates a chained "return result across more than one popped screen": selecting a car
    /// pushes <see cref="Phase12ScreenIds.Customization"/>; when that screen's "Done" button pops
    /// back here (<see cref="OnShown"/> fires, Phase 3's existing "screen became visible again"
    /// hook), this screen pops itself too on the next frame, this time carrying the chosen car as
    /// the result <see cref="CharacterSelectionScreen"/> registered a callback for when it
    /// originally pushed this screen.
    ///
    /// <b>Sharp edge this demonstrates on purpose:</b> <see cref="OnShown"/> fires synchronously
    /// from inside <see cref="INavigationService"/>'s own in-progress pop (the same call stack that
    /// is still closing Customization), so <see cref="INavigationService.IsNavigating"/> is still
    /// true at that point - calling <c>NavigateBack</c> directly from here is rejected as
    /// <see cref="NavigationResultKind.AlreadyActive"/>, the same deliberate re-entrancy guard
    /// <c>GameFlow.GameFlowService</c>/<c>Tutorials.TutorialService</c> already apply to commands
    /// issued from inside one of their own event handlers. Deferring one frame (a coroutine, exactly
    /// like this) is the correct way to chain a follow-up navigation call from a lifecycle hook.
    /// </summary>
    public sealed class CarSelectionScreen : UIScreen, IUINavigationParameterReceiver
    {
        [SerializeField] private Button _selectSportsCarButton;
        [SerializeField] private Button _backButton;

        private string _selectedCharacter;
        private string _chosenCar;

        private void OnEnable()
        {
            if (_selectSportsCarButton != null) _selectSportsCarButton.onClick.AddListener(OnSelectSportsCarClicked);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            if (_selectSportsCarButton != null) _selectSportsCarButton.onClick.RemoveListener(OnSelectSportsCarClicked);
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
        }

        public void OnNavigationParameters(object parameters) => _selectedCharacter = parameters as string;

        protected override void OnShown() => StartCoroutine(ContinueBackNextFrame());

        private IEnumerator ContinueBackNextFrame()
        {
            yield return null;
            Phase12DemoServices.Navigation?.NavigateBack(_chosenCar);
        }

        private void OnSelectSportsCarClicked()
        {
            _chosenCar = "SportsCar";
            Debug.Log($"[Phase12Demo] Character '{_selectedCharacter}' selected a Sports Car.");
            Phase12DemoServices.Navigation?.Navigate(Phase12ScreenIds.Customization, new NavigationRequestOptions(parameters: _chosenCar));
        }

        private void OnBackClicked() => Phase12DemoServices.Navigation?.NavigateBack();
    }
}
