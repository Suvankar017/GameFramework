namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Optional gate a game registers via <see cref="INavigationService.AddGuard"/> to reject or
    /// defer navigation under some condition - "unsaved changes", "loading in progress", "purchase
    /// currently processing" (CLAUDE.md's Phase 12 brief, section 17). Every registered guard runs,
    /// in registration order, for every screen/popup navigation request including back navigation;
    /// the first non-<see cref="NavigationGuardResult.Allow"/> result short-circuits the rest.
    /// </summary>
    public interface INavigationGuard
    {
        NavigationGuardResult Evaluate(in NavigationGuardContext context, out string reason);
    }
}
