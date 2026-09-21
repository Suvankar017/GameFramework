namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// The outcome of a navigation command. Every <see cref="INavigationService"/> command returns
    /// one of these instead of throwing - normal UI flow (a player mashing "back" twice, a duplicate
    /// button tap while a transition is playing) never needs a try/catch, the same pattern
    /// <c>GameFlow.IGameFlowService</c>'s commands already established.
    /// </summary>
    public readonly struct NavigationResult
    {
        public readonly NavigationResultKind Kind;

        /// <summary>Free-form detail for logging/diagnostics - never parsed, never shown directly
        /// to a player.</summary>
        public readonly string Reason;

        private NavigationResult(NavigationResultKind kind, string reason)
        {
            Kind = kind;
            Reason = reason;
        }

        public bool Success => Kind == NavigationResultKind.Success;

        public static NavigationResult Ok() => new NavigationResult(NavigationResultKind.Success, null);
        public static NavigationResult Block(string reason) => new NavigationResult(NavigationResultKind.Blocked, reason);
        public static NavigationResult Cancel(string reason = null) => new NavigationResult(NavigationResultKind.Cancelled, reason);
        public static NavigationResult Fail(string reason) => new NavigationResult(NavigationResultKind.Failed, reason);
        public static NavigationResult AlreadyActive() => new NavigationResult(NavigationResultKind.AlreadyActive, "A navigation operation is already in progress.");
        public static NavigationResult NotFound(string reason) => new NavigationResult(NavigationResultKind.NotFound, reason);
        public static NavigationResult Deferred(string reason) => new NavigationResult(NavigationResultKind.Deferred, reason);
    }
}
