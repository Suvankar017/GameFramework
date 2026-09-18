namespace GameFramework.GameFlow.Session
{
    /// <summary>Lifecycle of one <see cref="GameplaySession"/>. Mutated exclusively by
    /// <see cref="GameFlowService"/> - see that type and <see cref="GameplaySession"/>'s remarks on
    /// session ownership.</summary>
    public enum GameplaySessionState
    {
        /// <summary>Allocated but not yet started.</summary>
        Created,

        /// <summary>Actively simulating.</summary>
        Active,

        /// <summary>Active but not simulating - <see cref="GameplaySession.ElapsedGameplayTime"/>
        /// does not advance.</summary>
        Paused,

        Completed,
        Failed,

        /// <summary>Ended without a normal Complete/Fail - e.g. the level was exited or restarted
        /// while this session was still active.</summary>
        Aborted,

        /// <summary>Terminal - resources released, checkpoints cleared.</summary>
        Disposed
    }
}
