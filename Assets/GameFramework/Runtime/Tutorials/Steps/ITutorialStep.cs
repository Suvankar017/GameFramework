using System;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// One unit of tutorial progression - see CLAUDE.md's Phase 9 brief, sections 8-9, for the
    /// step model and the built-in step types (<see cref="InstructionStep"/>, <see cref="WaitStep"/>,
    /// <see cref="InputStep"/>, <see cref="EventStep{TEvent}"/>, <see cref="ConditionStep"/>).
    /// <see cref="TutorialService"/> guarantees only one step is ever active at a time and that
    /// <see cref="Begin"/>/<see cref="Complete"/>/<see cref="Cancel"/> are each only meaningful once
    /// per run - see <see cref="TutorialStepBase"/> for the shared idempotent implementation every
    /// built-in step uses.
    /// </summary>
    public interface ITutorialStep
    {
        /// <summary>Stable id matching the paired <see cref="TutorialStepDefinition.Id"/> - see
        /// <see cref="TutorialStepEntry"/>.</summary>
        string Id { get; }

        TutorialStepState State { get; }

        /// <summary>Raised exactly once, the moment this step transitions to
        /// <see cref="TutorialStepState.Completed"/> - by whatever trigger caused it (input,
        /// event, condition, a duration elapsing, or an explicit
        /// <see cref="ITutorialService.CompleteCurrentStep"/> call). <see cref="TutorialService"/>
        /// is the only subscriber in normal use; it advances to the next step (or finishes the
        /// tutorial) from this callback.</summary>
        event Action Completed;

        /// <summary>Called once when this step becomes the active step. Steps that need to wire up
        /// a subscription (see <see cref="EventStep{TEvent}"/>) or an initial check (see
        /// <see cref="ConditionStep"/>) do it here.</summary>
        void Begin();

        /// <summary>Called once per frame by <see cref="TutorialService"/> while this step is
        /// active and the tutorial is not <see cref="TutorialState.Paused"/>, with
        /// <see cref="Runtime.Time.ITimeService.UnscaledDeltaTime"/> - so a duration-based step
        /// (<see cref="WaitStep"/>, an <see cref="InstructionStep"/> with an auto-advance delay)
        /// keeps a stable, gameplay-pause-independent notion of elapsed time. Most step types leave
        /// this a no-op and complete themselves from a subscription instead - see
        /// CLAUDE.md's Phase 9 brief, section 8: "Do not force every step to use Update polling."</summary>
        void Tick(float unscaledDeltaTime);

        /// <summary>Force-completes this step if it is currently <see cref="TutorialStepState.Active"/>;
        /// a no-op otherwise (idempotent - see <see cref="TutorialStepBase"/>). This is both how a
        /// step completes itself internally and how <see cref="ITutorialService.CompleteCurrentStep"/>
        /// completes it externally - the same one method either way.</summary>
        void Complete();

        /// <summary>Force-ends this step without completing it (tutorial skipped/cancelled). A
        /// no-op if already <see cref="TutorialStepState.Completed"/>/<see cref="TutorialStepState.Cancelled"/>.
        /// Never raises <see cref="Completed"/>.</summary>
        void Cancel();

        /// <summary>
        /// Returns this step to <see cref="TutorialStepState.NotStarted"/> so it can be
        /// <see cref="Begin"/>-ed again - the same step *instances* passed to
        /// <see cref="ITutorialService.RegisterTutorial"/> are reused across every run of that
        /// tutorial (a <see cref="TutorialRepeatPolicy.Repeatable"/> replay,
        /// <see cref="ITutorialService.Restart"/>, or a <see cref="ITutorialService.Cancel"/>
        /// followed by a later <see cref="ITutorialService.Start"/>), so without this a step that
        /// already reached <see cref="TutorialStepState.Completed"/>/<see cref="TutorialStepState.Cancelled"/>
        /// once would silently refuse to run again. <see cref="TutorialService"/> calls this on every
        /// step of a tutorial immediately before starting a new run - a no-op if already
        /// <see cref="TutorialStepState.NotStarted"/>.
        /// </summary>
        void Reset();
    }
}
