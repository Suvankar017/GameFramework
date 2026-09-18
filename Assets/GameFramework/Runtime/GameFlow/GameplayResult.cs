namespace GameFramework.GameFlow
{
    /// <summary>
    /// Generic outcome of a gameplay session/level attempt. <see cref="ReasonId"/> is a free-form,
    /// game-defined identifier ("Finished", "Timeout", "PlayerDied", "OutOfMoves", "VehicleDestroyed",
    /// ...) - the framework hard-codes no concrete win/lose reasons, matching every other
    /// reason-carrying API in this codebase (e.g. <c>IEconomyService.TryAdd</c>'s <c>reason</c>).
    /// </summary>
    public readonly struct GameplayResult
    {
        /// <summary>Equivalent to <c>default(GameplayResult)</c> - see <see cref="GameplayResultKind.Unspecified"/>.</summary>
        public static readonly GameplayResult None = default;

        public readonly GameplayResultKind Kind;
        public readonly string ReasonId;

        public GameplayResult(GameplayResultKind kind, string reasonId = null)
        {
            Kind = kind;
            ReasonId = reasonId;
        }

        public bool IsUnspecified => Kind == GameplayResultKind.Unspecified;

        public static GameplayResult Success(string reasonId = null) => new GameplayResult(GameplayResultKind.Success, reasonId);
        public static GameplayResult Failure(string reasonId = null) => new GameplayResult(GameplayResultKind.Failure, reasonId);
        public static GameplayResult Aborted(string reasonId = null) => new GameplayResult(GameplayResultKind.Aborted, reasonId);
        public static GameplayResult Cancelled(string reasonId = null) => new GameplayResult(GameplayResultKind.Cancelled, reasonId);

        public override string ToString() => string.IsNullOrEmpty(ReasonId) ? Kind.ToString() : $"{Kind}:{ReasonId}";
    }
}
