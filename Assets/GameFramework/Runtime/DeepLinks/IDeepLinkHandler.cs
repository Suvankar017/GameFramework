namespace GameFramework.DeepLinks
{
    /// <summary>
    /// The generic extension point for reacting to a deep link - see CLAUDE.md's Phase 18 brief,
    /// section 20. This framework does not itself navigate/instantiate UI (sections 20/24); a game
    /// implements this to translate a link into whatever action it decides (typically calling
    /// <c>UI.Navigation.INavigationService</c> - see the optional
    /// <c>Notifications.Integration.NavigationDeepLinkHandler</c> for a ready-made example).
    /// </summary>
    public interface IDeepLinkHandler
    {
        bool CanHandle(DeepLink link);

        /// <summary>Only called when <see cref="CanHandle"/> just returned true for the same link.
        /// An exception thrown here is caught and logged by <see cref="DeepLinkService"/> and treated
        /// as <see cref="DeepLinkHandlerResult.Failed"/> - a handler bug must not crash the app.</summary>
        DeepLinkHandlerResult Handle(DeepLink link);
    }
}
