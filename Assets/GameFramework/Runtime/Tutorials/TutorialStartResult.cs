namespace GameFramework.Tutorials
{
    /// <summary>Outcome of <see cref="ITutorialService.Start"/>. Never throws for a normal
    /// invalid/disallowed request - see <see cref="TutorialService"/>'s remarks.</summary>
    public enum TutorialStartResult
    {
        Success,

        /// <summary>No tutorial with that <see cref="TutorialId"/> was registered via
        /// <see cref="ITutorialService.RegisterTutorial"/>.</summary>
        NotFound,

        /// <summary>The requested tutorial is already the active one.</summary>
        AlreadyRunning,

        /// <summary>Already completed and its <see cref="TutorialRepeatPolicy"/> does not allow
        /// starting again right now.</summary>
        AlreadyCompleted,

        /// <summary>One or more of <see cref="TutorialDefinition.Prerequisites"/> has not been
        /// completed yet.</summary>
        PrerequisiteNotMet,

        /// <summary>Rejected because a *different* tutorial is currently active (only one active
        /// tutorial is supported - see <see cref="ITutorialService"/>'s remarks), or because another
        /// command is already being processed.</summary>
        Blocked
    }
}
