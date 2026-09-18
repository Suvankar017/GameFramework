namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// Displays an instruction (the paired <see cref="TutorialStepDefinition.LocalizationKey"/> -
    /// this step carries no text itself, see CLAUDE.md's Phase 9 brief section 24) and completes
    /// either when explicitly acknowledged (<see cref="ITutorialService.CompleteCurrentStep"/> - a
    /// player tapping "Got it", or arbitrary game code deciding the instruction's condition was
    /// met) or, if <paramref name="autoAdvanceDelaySeconds"/> is greater than zero, automatically
    /// after that many unscaled seconds. With no auto-advance delay (the default), this is also the
    /// framework's "Manual/External Completion Step" - a pure gate with no built-in timeout,
    /// completed only by <see cref="ITutorialService.CompleteCurrentStep"/> or a game calling
    /// <see cref="Steps.ITutorialStep.Complete"/> directly.
    /// </summary>
    public sealed class InstructionStep : TutorialStepBase
    {
        private readonly float _autoAdvanceDelaySeconds;
        private float _elapsedSeconds;

        public InstructionStep(string id, float autoAdvanceDelaySeconds = 0f) : base(id)
        {
            _autoAdvanceDelaySeconds = autoAdvanceDelaySeconds;
        }

        protected override void OnBegin()
        {
            _elapsedSeconds = 0f;
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (_autoAdvanceDelaySeconds <= 0f)
            {
                return;
            }

            _elapsedSeconds += unscaledDeltaTime;
            if (_elapsedSeconds >= _autoAdvanceDelaySeconds)
            {
                Complete();
            }
        }
    }
}
