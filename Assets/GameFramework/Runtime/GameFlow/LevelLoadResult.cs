namespace GameFramework.GameFlow
{
    /// <summary>Immediate outcome of <see cref="IGameFlowService.LoadLevel"/>. Loading itself is
    /// asynchronous - watch <see cref="LevelReadyEvent"/> (or <see cref="IGameFlowService.StateChanged"/>)
    /// for actual completion.</summary>
    public enum LevelLoadResult
    {
        /// <summary>The load was accepted and is now in flight.</summary>
        Started,

        /// <summary>Rejected - the flow is not currently <see cref="LevelFlowState.Unloaded"/> (only
        /// one level flow is active at a time; see <see cref="GameFlowService"/>'s remarks on
        /// concurrent load requests).</summary>
        Rejected,

        /// <summary>The supplied <see cref="LevelDefinition"/> was null, had no <see cref="LevelId"/>,
        /// or had no scene name.</summary>
        InvalidLevel
    }
}
