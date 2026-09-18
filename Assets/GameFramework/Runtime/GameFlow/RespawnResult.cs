namespace GameFramework.GameFlow
{
    /// <summary>Outcome of <see cref="IGameFlowService.Respawn"/>. Never throws, and never leaves
    /// gameplay half-restored: GameFlow itself only ever touches its own state when a valid
    /// checkpoint is found - restoring the actual game world happens in code that reacts to
    /// <see cref="PlayerRespawnedEvent"/>, which owns any partial-failure handling of its own.</summary>
    public enum RespawnResult
    {
        Success,

        /// <summary>No level/session is currently active.</summary>
        NoActiveSession,

        /// <summary>The active session has no current checkpoint to respawn from - a deterministic
        /// fallback (e.g. <see cref="IGameFlowService.Retry"/>) is the caller's decision.</summary>
        NoCheckpoint,

        /// <summary>The current <see cref="LevelFlowState"/> does not allow a respawn (only
        /// <see cref="LevelFlowState.Playing"/> and <see cref="LevelFlowState.Failed"/> do).</summary>
        InvalidState,

        /// <summary>Another command is already being processed.</summary>
        Blocked
    }
}
