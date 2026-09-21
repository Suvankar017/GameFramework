using System.Collections.Generic;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Read-only development diagnostic snapshot - the Phase 12 equivalent of
    /// <c>Gameplay.Pooling.GameObjectPool.Statistics</c>. Allocates (one array per stack) each call,
    /// so it exists for a diagnostics overlay/inspector window, never a per-frame or gameplay-logic
    /// call (CLAUDE.md's Phase 12 brief, section 33).
    /// </summary>
    public readonly struct NavigationDiagnosticsSnapshot
    {
        public readonly UIScreenId CurrentScreen;
        public readonly IReadOnlyList<UIScreenId> ScreenStack;
        public readonly IReadOnlyList<UIPopupId> PopupStack;
        public readonly bool IsNavigating;
        public readonly int RegisteredScreenCount;
        public readonly int RegisteredPopupCount;

        public NavigationDiagnosticsSnapshot(
            UIScreenId currentScreen,
            IReadOnlyList<UIScreenId> screenStack,
            IReadOnlyList<UIPopupId> popupStack,
            bool isNavigating,
            int registeredScreenCount,
            int registeredPopupCount)
        {
            CurrentScreen = currentScreen;
            ScreenStack = screenStack;
            PopupStack = popupStack;
            IsNavigating = isNavigating;
            RegisteredScreenCount = registeredScreenCount;
            RegisteredPopupCount = registeredPopupCount;
        }
    }
}
