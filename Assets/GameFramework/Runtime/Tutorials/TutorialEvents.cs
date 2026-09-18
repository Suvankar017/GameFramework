namespace GameFramework.Tutorials
{
    /// <summary>
    /// Published by <see cref="TutorialService"/> through the existing <see cref="Runtime.Events.IEventService"/> -
    /// no separate notification mechanism. UI/presentation code observes these instead of the
    /// framework depending on any concrete UI type - see CLAUDE.md's Phase 9 brief, section 22.
    /// All immutable readonly structs carrying ids/context only, never a mutable reference into
    /// live tutorial state.
    /// </summary>
    public readonly struct TutorialStartedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialStartedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    public readonly struct TutorialStepStartedEvent
    {
        public readonly TutorialId TutorialId;
        public readonly string StepId;
        public readonly int StepIndex;
        public readonly int StepCount;

        public TutorialStepStartedEvent(TutorialId tutorialId, string stepId, int stepIndex, int stepCount)
        {
            TutorialId = tutorialId;
            StepId = stepId;
            StepIndex = stepIndex;
            StepCount = stepCount;
        }
    }

    public readonly struct TutorialStepCompletedEvent
    {
        public readonly TutorialId TutorialId;
        public readonly string StepId;
        public readonly int StepIndex;

        public TutorialStepCompletedEvent(TutorialId tutorialId, string stepId, int stepIndex)
        {
            TutorialId = tutorialId;
            StepId = stepId;
            StepIndex = stepIndex;
        }
    }

    /// <summary>Published when the tutorial's own <see cref="TutorialState"/> reactively mirrors
    /// <see cref="Runtime.Time.ITimeService.IsPaused"/> becoming true while running - see
    /// <see cref="TutorialService"/>'s remarks on pause.</summary>
    public readonly struct TutorialPausedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialPausedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    public readonly struct TutorialResumedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialResumedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    /// <summary>Published on a normal finish (every step completed). Not published for a skipped
    /// run - see <see cref="TutorialSkippedEvent"/> - or a cancelled one - see
    /// <see cref="TutorialCancelledEvent"/>.</summary>
    public readonly struct TutorialCompletedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialCompletedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    /// <summary>Published instead of <see cref="TutorialCompletedEvent"/> when
    /// <see cref="ITutorialService.Skip"/> ended the run.</summary>
    public readonly struct TutorialSkippedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialSkippedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    /// <summary>Published when <see cref="ITutorialService.Cancel"/> ended the run. Unlike a
    /// completion or a skip, a cancelled tutorial is not marked completed - see
    /// <see cref="TutorialService"/>'s remarks on <see cref="ITutorialService.Cancel"/>.</summary>
    public readonly struct TutorialCancelledEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialCancelledEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }

    /// <summary>Published once, the moment <see cref="ITutorialService.Restart"/> is accepted -
    /// before the new run's own <see cref="TutorialStartedEvent"/> follows.</summary>
    public readonly struct TutorialRestartedEvent
    {
        public readonly TutorialId TutorialId;
        public TutorialRestartedEvent(TutorialId tutorialId) => TutorialId = tutorialId;
    }
}
