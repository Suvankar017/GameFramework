using GameFramework.UI;
using GameFramework.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>Demonstrates passing a typed parameter forward via <see cref="NavigationRequestOptions.Parameters"/>
    /// and receiving a typed result back via <see cref="NavigationRequestOptions.ResultCallback"/> once
    /// the destination (and everything pushed on top of it) is eventually popped.</summary>
    public sealed class CharacterSelectionScreen : UIScreen, IUINavigationParameterReceiver
    {
        [SerializeField] private Button _selectWarriorButton;
        [SerializeField] private Button _backButton;

        private void OnEnable()
        {
            if (_selectWarriorButton != null) _selectWarriorButton.onClick.AddListener(OnSelectWarriorClicked);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            if (_selectWarriorButton != null) _selectWarriorButton.onClick.RemoveListener(OnSelectWarriorClicked);
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
        }

        // MainMenuScreen doesn't pass parameters into this screen, but it could - shown here purely
        // so this type also demonstrates the receiver side once, in addition to the sender side below.
        public void OnNavigationParameters(object parameters)
        {
        }

        private void OnSelectWarriorClicked()
        {
            var options = new NavigationRequestOptions(
                parameters: "Warrior",
                resultCallback: result => Debug.Log($"[Phase12Demo] Character flow finished with car '{result}'."));

            Phase12DemoServices.Navigation?.Navigate(Phase12ScreenIds.CarSelection, options);
        }

        private void OnBackClicked() => Phase12DemoServices.Navigation?.NavigateBack();
    }
}
