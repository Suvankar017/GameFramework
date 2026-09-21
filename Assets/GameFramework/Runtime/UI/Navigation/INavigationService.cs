using System;
using GameFramework.Runtime.Services;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Reusable UI navigation/menu-flow orchestration - the Phase 12 equivalent of
    /// <c>GameFlow.IGameFlowService</c>/<c>Presentation.IPresentationService</c>: one narrow,
    /// registered service that sits above Phase 3's <see cref="IUIService"/> (screens/popups/layered
    /// canvases) and adds the pieces a production mobile menu flow needs that Phase 3 deliberately
    /// left out - stable-id screen/popup registration, a navigation stack independent of Unity's own
    /// scene history, back-navigation priority (popup -> screen handler -> stack -> app), typed
    /// parameters/results, guards, and centralized Android/back-button routing.
    ///
    /// Every command returns a <see cref="NavigationResult"/> instead of throwing for an
    /// invalid/currently-disallowed request, the same pattern <c>IGameFlowService</c> already
    /// established. This is explicitly <b>not</b> a replacement for <see cref="IUIService"/> - every
    /// screen/popup instance this service manages is still opened/closed through it (see
    /// <see cref="IUIService.OpenScreen{T}"/>'s <c>onBeforeOpen</c> seam), never duplicated.
    /// </summary>
    public interface INavigationService : IGameService
    {
        /// <summary>True while a navigation command's stack mutation and/or its destination's
        /// <see cref="IUINavigationTransitionHandler"/> enter transition is still in flight. A
        /// second navigation command issued while true is rejected
        /// (<see cref="NavigationResultKind.AlreadyActive"/>) rather than queued or cancelled - see
        /// <see cref="NavigationResultKind.AlreadyActive"/>'s remarks.</summary>
        bool IsNavigating { get; }

        /// <summary>True if <see cref="NavigateBack"/> would do anything other than publish
        /// <see cref="BackRequestedAtRootEvent"/> - an open popup, or more than one screen on the
        /// stack.</summary>
        bool CanNavigateBack { get; }

        bool HasOpenPopups { get; }

        UIScreenId CurrentScreenId { get; }
        UIScreen CurrentScreen { get; }

        /// <summary>The most-recently-opened popup, or null/<see cref="UIPopupId.None"/> if none is open.</summary>
        UIPopupId CurrentPopupId { get; }
        UIPopup CurrentPopup { get; }

        /// <summary>Raised after every successful screen-stack change (push/replace/reset/pop) -
        /// the same information published as <see cref="ScreenNavigatedEvent"/>, offered directly
        /// for code that already holds this service rather than subscribing through
        /// <c>Runtime.Events.IEventService</c>.</summary>
        event Action<UIScreenId, UIScreenId> ScreenChanged;

        /// <summary>Registers <paramref name="prefab"/> under <paramref name="id"/>. Call once per
        /// id at composition-root time. Throws <see cref="ArgumentException"/> for an invalid id and
        /// <see cref="InvalidOperationException"/> for a duplicate - both authoring/programmer
        /// errors caught once at startup, matching <c>Presentation.IPresentationService.RegisterDefinition</c>.</summary>
        void RegisterScreen(UIScreenId id, UIScreen prefab);
        void UnregisterScreen(UIScreenId id);
        bool IsScreenRegistered(UIScreenId id);

        void RegisterPopup(UIPopupId id, UIPopup prefab);
        void UnregisterPopup(UIPopupId id);
        bool IsPopupRegistered(UIPopupId id);

        /// <summary>Pushes <paramref name="destination"/> on top of the screen stack.</summary>
        NavigationResult Navigate(UIScreenId destination, NavigationRequestOptions options = default);

        /// <summary>Replaces the current top screen with <paramref name="destination"/> without
        /// retaining it in history.</summary>
        NavigationResult Replace(UIScreenId destination, NavigationRequestOptions options = default);

        /// <summary>Clears the entire screen stack and establishes <paramref name="destination"/> as
        /// the new root - e.g. Main Menu -> Gameplay, where back should never return to the menu.</summary>
        NavigationResult Reset(UIScreenId destination, NavigationRequestOptions options = default);

        /// <summary>
        /// Implements the back-navigation priority chain (CLAUDE.md's Phase 12 brief, section 11):
        /// (1) an open popup closes first, (2) the current screen's own
        /// <see cref="IUINavigationBackHandler"/> gets a chance to consume the request, (3) otherwise
        /// the screen stack pops, (4) at the root with nothing left, <see cref="BackRequestedAtRootEvent"/>
        /// is published for the game/GameFlow/application to decide what "back at the root" means.
        /// <paramref name="result"/> is delivered to the popped entry's
        /// <see cref="NavigationRequestOptions.ResultCallback"/>, if any.
        /// </summary>
        NavigationResult NavigateBack(object result = null);

        NavigationResult OpenPopup(UIPopupId id, NavigationRequestOptions options = default);

        /// <summary>Closes the most-recently-opened popup. <paramref name="result"/> is delivered
        /// to its <see cref="NavigationRequestOptions.ResultCallback"/>, if any, and is treated as a
        /// confirmation (<see cref="UIPopupResult.Confirmed"/>) when non-null, else
        /// <see cref="UIPopupResult.None"/> - a plain back-navigation close is always
        /// <see cref="UIPopupResult.Cancelled"/> instead, see <see cref="NavigateBack"/>.</summary>
        NavigationResult CloseTopPopup(object result = null);

        void AddGuard(INavigationGuard guard);
        void RemoveGuard(INavigationGuard guard);

        /// <summary>
        /// Subscribes <paramref name="id"/>'s popup to open automatically whenever
        /// <typeparamref name="TEvent"/> is published through the existing
        /// <c>Runtime.Events.IEventService</c> - the same explicit, strongly-typed mapping
        /// <c>Presentation.IPresentationService.RegisterMapping</c> established, applied here to
        /// GameFlow-driven (or any event-driven) UI. Throws <see cref="InvalidOperationException"/>
        /// if <typeparamref name="TEvent"/> already has a registered mapping.
        /// </summary>
        void RegisterEventMapping<TEvent>(UIPopupId id, Func<TEvent, NavigationRequestOptions> optionsFactory = null);

        void UnregisterEventMapping<TEvent>();

        NavigationDiagnosticsSnapshot GetDiagnostics();
    }
}
