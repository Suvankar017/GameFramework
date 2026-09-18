namespace GameFramework.Tutorials.Steps
{
    /// <summary>Lifecycle of one <see cref="ITutorialStep"/>: NotStarted -> Active -> (Completed |
    /// Cancelled). No Skipped state - skipping is a tutorial-level operation (see
    /// <see cref="TutorialState"/>'s remarks), not a per-step one.</summary>
    public enum TutorialStepState
    {
        NotStarted,
        Active,
        Completed,
        Cancelled
    }
}
