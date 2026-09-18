namespace GameFramework.GameFlow
{
    /// <summary>Outcome of a requested <see cref="LevelFlowState"/> transition or flow command.
    /// Normal gameplay flow never throws for an invalid/blocked command - see
    /// <see cref="LevelFlowStateMachine"/> and <see cref="GameFlowService"/>.</summary>
    public enum TransitionResult
    {
        Success,

        /// <summary>The requested target is already the current state.</summary>
        AlreadyInState,

        /// <summary>The current state has no allowed transition to the requested target.</summary>
        InvalidTransition,

        /// <summary>Rejected because another command/transition is already being processed - e.g.
        /// a command issued from inside an event handler triggered by an in-progress command.</summary>
        TransitionBlocked
    }
}
