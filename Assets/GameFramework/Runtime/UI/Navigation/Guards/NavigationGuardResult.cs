namespace GameFramework.UI.Navigation
{
    /// <summary>See <see cref="INavigationGuard"/>'s remarks (CLAUDE.md's Phase 12 brief, section 17).</summary>
    public enum NavigationGuardResult
    {
        Allow,
        Block,

        /// <summary>Asks the request to wait rather than proceed or reject outright (e.g. "purchase
        /// currently processing"). This layer does not queue or retry the request itself - it is
        /// reported as <see cref="NavigationResultKind.Deferred"/> and the guard (or the game) is
        /// responsible for calling the navigation method again once ready. Deliberately not a
        /// generic workflow engine (CLAUDE.md's Phase 12 brief, section 17).</summary>
        Defer
    }
}
