using System;
using GameFramework.GameFlow;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>One entry on <see cref="NavigationService"/>'s own popup stack - see
    /// <see cref="NavigationEntry"/>'s remarks; additionally owns the optional pause token acquired
    /// for <see cref="NavigationRequestOptions.PausesGameplay"/>.</summary>
    internal sealed class PopupEntry
    {
        public UIPopupId Id;
        public UIPopup Popup;
        public Action<object> ResultCallback;
        public IPauseToken PauseToken;
    }
}
