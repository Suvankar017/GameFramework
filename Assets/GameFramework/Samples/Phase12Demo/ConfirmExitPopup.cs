using GameFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>Demonstrates the nested-popup-stack back priority (CLAUDE.md's Phase 12 brief,
    /// section 16): while this is open, a back-navigation request closes only this popup first,
    /// leaving <see cref="SettingsPopup"/> (still beneath it) open.</summary>
    public sealed class ConfirmExitPopup : UIPopup
    {
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;

        private void OnEnable()
        {
            if (_yesButton != null) _yesButton.onClick.AddListener(OnYesClicked);
            if (_noButton != null) _noButton.onClick.AddListener(OnNoClicked);
        }

        private void OnDisable()
        {
            if (_yesButton != null) _yesButton.onClick.RemoveListener(OnYesClicked);
            if (_noButton != null) _noButton.onClick.RemoveListener(OnNoClicked);
        }

        private void OnYesClicked()
        {
            Debug.Log("[Phase12Demo] Exit confirmed (Application.Quit() intentionally not called in this sample).");
            Phase12DemoServices.Navigation?.CloseTopPopup(); // closes this popup
            Phase12DemoServices.Navigation?.CloseTopPopup(); // closes Settings underneath it
        }

        private void OnNoClicked() => Phase12DemoServices.Navigation?.CloseTopPopup();
    }
}
