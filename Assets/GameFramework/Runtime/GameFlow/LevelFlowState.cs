namespace GameFramework.GameFlow
{
    /// <summary>
    /// The single state machine Phase 8 ships — combined level-load lifecycle and gameplay-session
    /// state, driven exclusively by <see cref="GameFlowService"/> (see <see cref="LevelFlowStateMachine"/>
    /// for the allowed-transition table). The framework defines no concrete application-level
    /// states (MainMenu, Boot, ...) — those remain <see cref="Runtime.State.IGameStateService"/>'s
    /// job, a level-independent, game-defined concept this type never touches.
    /// </summary>
    public enum LevelFlowState
    {
        /// <summary>No level is loaded. The starting state, and the state <see cref="GameFlowService.ExitLevel"/>
        /// always returns to.</summary>
        Unloaded,

        /// <summary>A scene load was requested and is in flight.</summary>
        Loading,

        /// <summary>The scene finished loading; game-specific setup (spawning, wiring references)
        /// runs in response to <see cref="LevelInitializingEvent"/>, published while this state is
        /// current.</summary>
        Initializing,

        /// <summary>Initialization is complete; gameplay has not started yet. Waits for an explicit
        /// <see cref="GameFlowService.StartLevel"/>.</summary>
        Ready,

        /// <summary>A <see cref="Session.GameplaySession"/> is active and not paused.</summary>
        Playing,

        /// <summary>A session is active but <see cref="Runtime.Time.ITimeService.IsPaused"/> is
        /// true. Detected reactively, exactly like <see cref="Gameplay.IGameplayService"/>'s own
        /// pause handling — nothing here ever writes <c>Time.timeScale</c> directly.</summary>
        Paused,

        /// <summary>Transient: the current attempt just succeeded and is being finalized.</summary>
        Completing,

        /// <summary>The current attempt succeeded and finalization is done.</summary>
        Completed,

        /// <summary>Transient: the current attempt just failed and is being finalized.</summary>
        Failing,

        /// <summary>The current attempt failed and finalization is done. The one state
        /// <see cref="GameFlowService.Respawn"/> can resume play from directly.</summary>
        Failed,

        /// <summary>Transient: a new attempt was requested (<see cref="GameFlowService.Retry"/>)
        /// and is being prepared - either a scene reload or an immediate reset back to
        /// <see cref="Ready"/>.</summary>
        Restarting,

        /// <summary>Transient: <see cref="GameFlowService.ExitLevel"/> is tearing down the current
        /// level/session.</summary>
        Exiting
    }
}
