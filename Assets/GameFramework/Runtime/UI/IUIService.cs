using System;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Reusable UI building blocks: layered canvases, a stack-navigable screen flow, and a
    /// popup/modal foundation with automatic input blocking. This is not a complete UI framework —
    /// no game-specific screens/menus/HUD live here (see Phase 3 scope). A higher-level navigation
    /// layer (Phase 12, <c>GameFramework.UI.Navigation</c>) orchestrates this stack rather than
    /// duplicating it - see <paramref name="onBeforeOpen"/> on <see cref="OpenScreen{T}"/>/
    /// <see cref="OpenPopup{T}"/>, the one seam that layer needed added here.
    /// </summary>
    public interface IUIService : IGameService
    {
        /// <summary>The root transform for a layer, e.g. to parent a HUD element that isn't a
        /// full <see cref="UIScreen"/>.</summary>
        Transform GetLayerRoot(UILayer layer);

        /// <summary>Instantiates <paramref name="prefab"/> under its own <see cref="UIScreen.Layer"/>
        /// and pushes it onto the screen stack, hiding the previous top screen (if any).
        /// <paramref name="onBeforeOpen"/>, if supplied, runs after instantiation but before
        /// <see cref="UIScreen.OnOpened"/> fires - the seam a caller (e.g. a navigation layer) uses
        /// to inject data the screen needs during its own opening lifecycle hook.</summary>
        T OpenScreen<T>(T prefab, Action<T> onBeforeOpen = null) where T : UIScreen;

        /// <summary>Closes <paramref name="screen"/> if it is the current top screen; otherwise a
        /// no-op (logged) — this stack does not support removing an arbitrary non-top entry.</summary>
        void CloseScreen(UIScreen screen);

        /// <summary>Closes the current top screen. Safe to call with an empty stack (no-op).</summary>
        void CloseTopScreen();

        /// <summary>Pops every screen down to the first one pushed.</summary>
        void PopToRoot();

        UIScreen CurrentScreen { get; }

        /// <summary>Instantiates <paramref name="prefab"/> under the Modal or Popup layer
        /// (depending on <see cref="UIPopup.IsModal"/>) and opens it. <paramref name="onBeforeOpen"/>
        /// behaves exactly as it does on <see cref="OpenScreen{T}"/>.</summary>
        T OpenPopup<T>(T prefab, Action<T> onBeforeOpen = null) where T : UIPopup;

        void ClosePopup(UIPopup popup, UIPopupResult result = UIPopupResult.None);

        /// <summary>Closes the most-recently-opened popup. Safe to call with none open (no-op).</summary>
        void CloseTopPopup(UIPopupResult result = UIPopupResult.None);

        UIPopup CurrentPopup { get; }
    }
}
