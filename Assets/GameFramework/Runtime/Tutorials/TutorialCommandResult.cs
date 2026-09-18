namespace GameFramework.Tutorials
{
    /// <summary>Outcome of a <see cref="ITutorialService"/> command other than <see cref="ITutorialService.Start"/>
    /// (<see cref="ITutorialService.Skip"/>/<see cref="ITutorialService.Cancel"/>/
    /// <see cref="ITutorialService.Restart"/>/<see cref="ITutorialService.CompleteCurrentStep"/>).
    /// Never thrown for a normal invalid/disallowed request.</summary>
    public enum TutorialCommandResult
    {
        Success,

        /// <summary>(<see cref="ITutorialService.Restart"/> only.) No tutorial with that
        /// <see cref="TutorialId"/> was registered.</summary>
        NotFound,

        /// <summary>No tutorial is currently active.</summary>
        NoActiveTutorial,

        /// <summary>The active tutorial (or its current step) is not in a state this command
        /// accepts.</summary>
        InvalidState,

        /// <summary>Rejected because another command is already being processed - e.g. a command
        /// issued from inside an event handler triggered by an in-progress command - or, for
        /// <see cref="ITutorialService.Restart"/>, because a *different* tutorial is currently
        /// active.</summary>
        Blocked,

        /// <summary>(<see cref="ITutorialService.Skip"/> only.) The active tutorial's
        /// <see cref="TutorialSkipPolicy"/> is <see cref="TutorialSkipPolicy.NotSkippable"/>.</summary>
        NotSkippable
    }
}
