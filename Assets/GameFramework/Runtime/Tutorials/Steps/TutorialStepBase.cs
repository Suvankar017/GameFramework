using System;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// Shared NotStarted -> Active -> (Completed | Cancelled) state machine every built-in step
    /// type is built on - the step-level equivalent of <see cref="Gameplay.Objectives.ObjectiveBase"/>.
    /// <see cref="Complete"/> is deliberately silent-idempotent rather than logging on a repeat call
    /// (unlike <see cref="Gameplay.Objectives.ObjectiveBase"/>'s invalid-transition warnings) because
    /// a step can legitimately be asked to complete from more than one place in normal operation -
    /// e.g. its own internal trigger firing in the same frame an external
    /// <see cref="ITutorialService.CompleteCurrentStep"/> call also arrives - and that race is
    /// expected to be harmless, not diagnostic.
    /// </summary>
    public abstract class TutorialStepBase : ITutorialStep
    {
        private const string LogCategory = "Tutorials";

        protected TutorialStepBase(string id)
        {
            Id = Guard.NotNullOrEmpty(id, nameof(id));
        }

        public string Id { get; }
        public TutorialStepState State { get; private set; } = TutorialStepState.NotStarted;

        public event Action Completed;

        public void Begin()
        {
            if (State != TutorialStepState.NotStarted)
            {
                Log.Warning(LogCategory, $"Tutorial step '{Id}' cannot Begin from state {State}; ignored.");
                return;
            }

            State = TutorialStepState.Active;
            OnBegin();
        }

        public virtual void Tick(float unscaledDeltaTime)
        {
        }

        public void Complete()
        {
            if (State != TutorialStepState.Active)
            {
                return;
            }

            State = TutorialStepState.Completed;
            OnEnd();
            Completed?.Invoke();
        }

        public void Cancel()
        {
            if (State == TutorialStepState.Completed || State == TutorialStepState.Cancelled)
            {
                return;
            }

            State = TutorialStepState.Cancelled;
            OnEnd();
        }

        public void Reset()
        {
            if (State == TutorialStepState.NotStarted)
            {
                return;
            }

            State = TutorialStepState.NotStarted;
            OnReset();
        }

        /// <summary>Called once, after <see cref="State"/> has already changed to Active.</summary>
        protected virtual void OnBegin()
        {
        }

        /// <summary>Called after <see cref="State"/> has already changed back to NotStarted. Built-in
        /// steps need no override here: any per-run transient value (an elapsed-time accumulator,
        /// say) is already re-initialized in <see cref="OnBegin"/>, which always runs again before
        /// that value is read.</summary>
        protected virtual void OnReset()
        {
        }

        /// <summary>Called exactly once, from either <see cref="Complete"/> or <see cref="Cancel"/> -
        /// the one place to release a subscription or other resource this step acquired in
        /// <see cref="OnBegin"/> (see <see cref="EventStep{TEvent}"/>).</summary>
        protected virtual void OnEnd()
        {
        }
    }
}
