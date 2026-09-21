using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Optional hook a <see cref="UIScreen"/> subclass implements to intercept a back-navigation
    /// request before it pops the stack - e.g. a screen with its own internal tabs/sub-panels that
    /// should close on the first back press rather than immediately leaving the whole screen
    /// (CLAUDE.md's Phase 12 brief, section 11, priority rung 3: "current screen-specific back
    /// handler").
    /// </summary>
    public interface IUINavigationBackHandler
    {
        /// <summary>Return true to consume the back request (no stack change); false to let normal
        /// back-navigation (popping the stack) proceed.</summary>
        bool OnBackRequested();
    }
}
