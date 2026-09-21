using GameFramework.UI;
using GameFramework.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Samples.Phase12Demo
{
    public sealed class CustomizationScreen : UIScreen, IUINavigationParameterReceiver
    {
        [SerializeField] private Button _doneButton;

        private void OnEnable()
        {
            if (_doneButton != null) _doneButton.onClick.AddListener(OnDoneClicked);
        }

        private void OnDisable()
        {
            if (_doneButton != null) _doneButton.onClick.RemoveListener(OnDoneClicked);
        }

        public void OnNavigationParameters(object parameters) =>
            Debug.Log($"[Phase12Demo] Customizing car '{parameters}'.");

        private void OnDoneClicked() => Phase12DemoServices.Navigation?.NavigateBack();
    }
}
