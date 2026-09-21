using GameFramework.UI;
using GameFramework.UI.Navigation;
using UnityEngine;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>
    /// Composition-root glue for this sample: registers every screen/popup prefab under its stable
    /// id, then establishes <see cref="Phase12ScreenIds.MainMenu"/> as the navigation root. Not part
    /// of the reusable framework - a real game does this from its own bootstrapper subclass instead
    /// of a separate MonoBehaviour, shown as its own component here only so this sample's wiring is
    /// visible/inspectable in the Editor.
    /// </summary>
    public sealed class Phase12DemoController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private UIScreen _mainMenu;
        [SerializeField] private UIScreen _characterSelection;
        [SerializeField] private UIScreen _carSelection;
        [SerializeField] private UIScreen _customization;

        [Header("Popups")]
        [SerializeField] private UIPopup _settings;
        [SerializeField] private UIPopup _confirmExit;

        private void Start()
        {
            INavigationService navigation = Phase12DemoServices.Navigation;
            if (navigation == null)
            {
                Debug.LogError("[Phase12Demo] INavigationService is not registered - is NavigationBootstrapper on this GameObject?");
                return;
            }

            navigation.RegisterScreen(Phase12ScreenIds.MainMenu, _mainMenu);
            navigation.RegisterScreen(Phase12ScreenIds.CharacterSelection, _characterSelection);
            navigation.RegisterScreen(Phase12ScreenIds.CarSelection, _carSelection);
            navigation.RegisterScreen(Phase12ScreenIds.Customization, _customization);

            navigation.RegisterPopup(Phase12ScreenIds.Settings, _settings);
            navigation.RegisterPopup(Phase12ScreenIds.ConfirmExit, _confirmExit);

            navigation.Reset(Phase12ScreenIds.MainMenu);
        }
    }
}
