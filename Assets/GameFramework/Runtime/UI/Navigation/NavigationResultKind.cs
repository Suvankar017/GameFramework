namespace GameFramework.UI.Navigation
{
    /// <summary>See <see cref="NavigationResult"/>'s remarks (CLAUDE.md's Phase 12 brief, section
    /// 18) - every navigation command returns one of these instead of throwing.</summary>
    public enum NavigationResultKind
    {
        Success,

        /// <summary>An <see cref="INavigationGuard"/> rejected the request outright.</summary>
        Blocked,

        /// <summary>The request was consumed without a stack change (e.g. a screen's own
        /// <see cref="IUINavigationBackHandler"/> handled a back request itself).</summary>
        Cancelled,

        Failed,

        /// <summary>A navigation operation is already in progress - see
        /// <see cref="INavigationService.IsNavigating"/>. Concurrent requests are rejected, not
        /// queued or cancelled (CLAUDE.md's Phase 12 brief, section 21) - retry once the in-flight
        /// operation completes.</summary>
        AlreadyActive,

        /// <summary>The destination id is not registered, or there is nothing left to navigate to
        /// (e.g. <see cref="INavigationService.NavigateBack"/> at the root of the stack).</summary>
        NotFound,

        /// <summary>An <see cref="INavigationGuard"/> asked the request to wait rather than proceed
        /// or reject outright - see <see cref="NavigationGuardResult.Defer"/>'s remarks. The guard
        /// itself is responsible for retrying the call once ready; this layer does not queue or
        /// retry automatically.</summary>
        Deferred
    }
}
