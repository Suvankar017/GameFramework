namespace GameFramework.UI.Navigation
{
    /// <summary>The minimum useful set of stack operations a screen navigation can perform
    /// (CLAUDE.md's Phase 12 brief, section 14).</summary>
    public enum NavigationMode
    {
        /// <summary>A -> B -> C: pushes a new entry on top of the stack.</summary>
        Push,

        /// <summary>A -> B without retaining A: replaces the current top entry.</summary>
        Replace,

        /// <summary>Clears the entire stack and establishes a new root - e.g. Main Menu -> Gameplay,
        /// where "back" should never return to the menu.</summary>
        Reset,

        /// <summary>Returns to the previous stack entry - reported on
        /// <see cref="NavigationGuardContext"/>/<see cref="ScreenNavigatedEvent"/> for a
        /// <see cref="INavigationService.NavigateBack"/> call; never passed directly to
        /// <see cref="INavigationService.Navigate"/>/<see cref="INavigationService.Replace"/>/
        /// <see cref="INavigationService.Reset"/>.</summary>
        Pop
    }
}
