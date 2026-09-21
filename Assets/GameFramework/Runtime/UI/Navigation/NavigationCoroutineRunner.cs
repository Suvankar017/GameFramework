using UnityEngine;

namespace GameFramework.UI.Navigation
{
    /// <summary>Bare coroutine host <see cref="NavigationService"/> creates for itself (like
    /// <c>UI.UIService</c> creating its own <see cref="UnityEngine.EventSystems.EventSystem"/>) so
    /// it can run <see cref="IUINavigationTransitionHandler"/> coroutines without needing a game
    /// object of its own to attach to.</summary>
    internal sealed class NavigationCoroutineRunner : MonoBehaviour
    {
    }
}
