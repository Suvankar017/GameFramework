using GameFramework.Input;
using UnityEngine;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// The one centralized place this framework reads a hardware/physical "back" signal
    /// (CLAUDE.md's Phase 12 brief, section 12 - "do not scatter Input.GetKeyDown(KeyCode.Escape)
    /// throughout the project"). <see cref="KeyCode.Escape"/> is Unity's documented mapping for the
    /// Android hardware/gesture back button as well as the desktop Escape key, so this alone gives
    /// zero-setup Android back support; if <see cref="IInputService"/> is registered, a logical
    /// action (default "Cancel", e.g. bound to a gamepad B button) is also honored - whichever
    /// fires first triggers <see cref="INavigationService.NavigateBack"/>, which itself implements
    /// the full popup -> screen-handler -> stack -> root-event priority chain.
    /// </summary>
    internal sealed class NavigationBackButtonDriver : MonoBehaviour
    {
        private NavigationService _owner;
        private IInputService _input;
        private string _actionName;

        internal void Initialize(NavigationService owner, IInputService input, string actionName)
        {
            _owner = owner;
            _input = input;
            _actionName = actionName;
        }

        private void Update()
        {
            bool triggered = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            triggered = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif

            if (!triggered && _input != null && !string.IsNullOrEmpty(_actionName))
            {
                triggered = _input.GetButtonDown(_actionName);
            }

            if (triggered)
            {
                _owner.NavigateBack();
            }
        }
    }
}
