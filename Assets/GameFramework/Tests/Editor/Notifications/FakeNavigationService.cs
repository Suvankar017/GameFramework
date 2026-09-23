using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;
using GameFramework.UI;
using GameFramework.UI.Navigation;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Fully controllable <see cref="INavigationService"/> test double - lets
    /// <c>NavigationDeepLinkHandler</c> be tested without a real UGUI screen stack (which would
    /// require PlayMode - see <c>UI.Navigation.Tests</c>'s own remarks). Only <see cref="Navigate"/>
    /// is meaningfully implemented; everything else is a minimal, harmless default.</summary>
    internal sealed class FakeNavigationService : INavigationService
    {
        public List<(UIScreenId Destination, NavigationRequestOptions Options)> NavigateCalls { get; } = new List<(UIScreenId, NavigationRequestOptions)>();
        public NavigationResult NextResult { get; set; } = NavigationResult.Ok();

        public bool IsNavigating => false;
        public bool CanNavigateBack => false;
        public bool HasOpenPopups => false;
        public UIScreenId CurrentScreenId => default;
        public UIScreen CurrentScreen => null;
        public UIPopupId CurrentPopupId => default;
        public UIPopup CurrentPopup => null;

        public event Action<UIScreenId, UIScreenId> ScreenChanged { add { } remove { } }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void RegisterScreen(UIScreenId id, UIScreen prefab)
        {
        }

        public void UnregisterScreen(UIScreenId id)
        {
        }

        public bool IsScreenRegistered(UIScreenId id) => false;

        public void RegisterPopup(UIPopupId id, UIPopup prefab)
        {
        }

        public void UnregisterPopup(UIPopupId id)
        {
        }

        public bool IsPopupRegistered(UIPopupId id) => false;

        public NavigationResult Navigate(UIScreenId destination, NavigationRequestOptions options = default)
        {
            NavigateCalls.Add((destination, options));
            return NextResult;
        }

        public NavigationResult Replace(UIScreenId destination, NavigationRequestOptions options = default) => NextResult;

        public NavigationResult Reset(UIScreenId destination, NavigationRequestOptions options = default) => NextResult;

        public NavigationResult NavigateBack(object result = null) => NextResult;

        public NavigationResult OpenPopup(UIPopupId id, NavigationRequestOptions options = default) => NextResult;

        public NavigationResult CloseTopPopup(object result = null) => NextResult;

        public void AddGuard(INavigationGuard guard)
        {
        }

        public void RemoveGuard(INavigationGuard guard)
        {
        }

        public void RegisterEventMapping<TEvent>(UIPopupId id, Func<TEvent, NavigationRequestOptions> optionsFactory = null)
        {
        }

        public void UnregisterEventMapping<TEvent>()
        {
        }

        public NavigationDiagnosticsSnapshot GetDiagnostics() => default;
    }
}
