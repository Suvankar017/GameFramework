namespace GameFramework.Tutorials
{
    /// <summary>
    /// Lifecycle of the one tutorial <see cref="TutorialService"/> can run at a time (see
    /// <see cref="ITutorialService"/>'s remarks on the "one active tutorial" policy). Driven
    /// exclusively by <see cref="TutorialService"/> through <see cref="TutorialLifecycleStateMachine"/>'s
    /// fixed allowed-transition table.
    ///
    /// Skipping is deliberately not a separate state - a skip completes the tutorial through the
    /// same Completing/Completed path as a normal finish, distinguished only by which event is
    /// published (<see cref="TutorialSkippedEvent"/> instead of <see cref="TutorialCompletedEvent"/>)
    /// and by the persisted "was skipped" flag. See CLAUDE.md's Phase 9 brief, section 5: "Do not
    /// create unnecessary states."
    /// </summary>
    public enum TutorialState
    {
        /// <summary>No tutorial is active. The starting state, and the state the service returns to
        /// immediately after every <see cref="Completed"/>/<see cref="Cancelled"/> outcome.</summary>
        Inactive,

        /// <summary>Transient: <see cref="ITutorialService.Start"/> was accepted and the run is
        /// being prepared (gameplay pause acquired if configured, first step begun).</summary>
        Starting,

        /// <summary>A tutorial run is active and not paused.</summary>
        Running,

        /// <summary>A tutorial run is active but <see cref="Runtime.Time.ITimeService.IsPaused"/> is
        /// true. Detected reactively every tick, exactly like <see cref="Runtime.Bootstrap.GameBootstrapper"/>'s
        /// other reactive-pause consumers - nothing here ever writes <c>Time.timeScale</c> directly.</summary>
        Paused,

        /// <summary>Transient: the run just finished (normally or via skip) and is being finalized.</summary>
        Completing,

        /// <summary>Terminal (briefly): finalization is done. The service auto-returns to
        /// <see cref="Inactive"/> immediately after publishing the completion event.</summary>
        Completed,

        /// <summary>Transient: <see cref="ITutorialService.Cancel"/> was accepted and the run is
        /// being torn down.</summary>
        Cancelling,

        /// <summary>Terminal (briefly): teardown is done. The service auto-returns to
        /// <see cref="Inactive"/> immediately after publishing <see cref="TutorialCancelledEvent"/>.</summary>
        Cancelled
    }
}
