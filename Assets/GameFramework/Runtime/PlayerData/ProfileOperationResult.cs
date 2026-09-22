namespace GameFramework.PlayerData
{
    /// <summary>
    /// The outcome of an <see cref="IPlayerProfileService"/> command. Every command returns one of
    /// these instead of throwing - normal flow (a duplicate load request, saving with nothing
    /// dirty's caller checking anyway, deleting a profile that never existed) never needs a
    /// try/catch, the same pattern <c>GameFlow.IGameFlowService</c>/<c>UI.Navigation.INavigationService</c>
    /// already established.
    /// </summary>
    public readonly struct ProfileOperationResult
    {
        public readonly ProfileOperationResultKind Kind;

        /// <summary>Free-form detail for logging/diagnostics - never parsed, never shown directly
        /// to a player.</summary>
        public readonly string Reason;

        private ProfileOperationResult(ProfileOperationResultKind kind, string reason)
        {
            Kind = kind;
            Reason = reason;
        }

        public bool Success => Kind == ProfileOperationResultKind.Success;

        public static ProfileOperationResult Ok(string reason = null) => new ProfileOperationResult(ProfileOperationResultKind.Success, reason);
        public static ProfileOperationResult AlreadyActive() => new ProfileOperationResult(ProfileOperationResultKind.AlreadyActive, "A profile operation is already in progress.");
        public static ProfileOperationResult NotFound(string reason) => new ProfileOperationResult(ProfileOperationResultKind.NotFound, reason);
        public static ProfileOperationResult AlreadyExists(string reason) => new ProfileOperationResult(ProfileOperationResultKind.AlreadyExists, reason);
        public static ProfileOperationResult InvalidState(string reason) => new ProfileOperationResult(ProfileOperationResultKind.InvalidState, reason);
        public static ProfileOperationResult Corrupted(string reason) => new ProfileOperationResult(ProfileOperationResultKind.Corrupted, reason);
        public static ProfileOperationResult Fail(string reason) => new ProfileOperationResult(ProfileOperationResultKind.Failed, reason);
    }
}
