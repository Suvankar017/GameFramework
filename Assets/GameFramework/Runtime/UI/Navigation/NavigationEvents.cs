using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>Published by <see cref="NavigationService"/> through the existing
    /// <c>Runtime.Events.IEventService</c> - no separate notification mechanism, matching every
    /// other cross-system notification in the framework.</summary>
    public readonly struct ScreenNavigatedEvent
    {
        public readonly UIScreenId Previous;
        public readonly UIScreenId Current;
        public readonly NavigationMode Mode;

        public ScreenNavigatedEvent(UIScreenId previous, UIScreenId current, NavigationMode mode)
        {
            Previous = previous;
            Current = current;
            Mode = mode;
        }
    }

    public readonly struct PopupOpenedEvent
    {
        public readonly UIPopupId Id;
        public PopupOpenedEvent(UIPopupId id) => Id = id;
    }

    public readonly struct PopupClosedEvent
    {
        public readonly UIPopupId Id;
        public readonly UIPopupResult Result;

        public PopupClosedEvent(UIPopupId id, UIPopupResult result)
        {
            Id = id;
            Result = result;
        }
    }

    /// <summary>Published when a back-navigation request reaches the root screen with no popup,
    /// screen back-handler, or navigation history left to consume it (CLAUDE.md's Phase 12 brief,
    /// section 11, priority rungs 4-6). A game subscribes to decide what "back at the root" means -
    /// open a pause/confirm-exit popup, forward to <c>GameFlow.IGameFlowService</c>, or call
    /// <see cref="UnityEngine.Application.Quit()"/> - this framework never assumes one.</summary>
    public readonly struct BackRequestedAtRootEvent
    {
        public readonly UIScreenId CurrentScreen;
        public BackRequestedAtRootEvent(UIScreenId currentScreen) => CurrentScreen = currentScreen;
    }

    /// <summary>Published whenever an <see cref="INavigationGuard"/> blocks a request - a
    /// development/analytics diagnostic, not something gameplay code should branch on (subscribe to
    /// the guard itself, or the returned <see cref="NavigationResult"/>, for that).</summary>
    public readonly struct NavigationBlockedEvent
    {
        public readonly UIScreenId Destination;
        public readonly NavigationResultKind Kind;
        public readonly string Reason;

        public NavigationBlockedEvent(UIScreenId destination, NavigationResultKind kind, string reason)
        {
            Destination = destination;
            Kind = kind;
            Reason = reason;
        }
    }
}
