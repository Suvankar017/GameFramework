namespace GameFramework.UI.Navigation
{
    /// <summary>Immutable snapshot describing one in-flight navigation request, passed to every
    /// registered <see cref="INavigationGuard"/> in registration order.</summary>
    public readonly struct NavigationGuardContext
    {
        /// <summary>The current top screen before this request, or <see cref="UIScreenId.None"/> if
        /// none is open yet.</summary>
        public readonly UIScreenId From;

        /// <summary>The requested destination, or <see cref="UIScreenId.None"/> for a back-navigation
        /// request (the destination isn't known until the stack is actually popped) or a popup open
        /// (popups are guarded too, but have no "destination screen").</summary>
        public readonly UIScreenId To;

        public readonly NavigationMode Mode;
        public readonly bool IsBackNavigation;

        public NavigationGuardContext(UIScreenId from, UIScreenId to, NavigationMode mode, bool isBackNavigation)
        {
            From = from;
            To = to;
            Mode = mode;
            IsBackNavigation = isBackNavigation;
        }
    }
}
