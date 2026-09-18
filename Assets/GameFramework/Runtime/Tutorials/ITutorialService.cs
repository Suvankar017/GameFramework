using System;
using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Services;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Application-level tutorial orchestrator - the Phase 9 equivalent of
    /// <see cref="GameFlow.IGameFlowService"/>/<c>Quests.IQuestService</c>: one narrow, registered
    /// service that owns which tutorial is currently running, its step sequence, and
    /// start/skip/cancel/restart/complete-step commands. It owns lifecycle/state/sequencing/
    /// persistence/events only - never what a tutorial actually teaches, which UI shows its
    /// instructions, or which localized text a step displays (see <see cref="TutorialStepDefinition"/>).
    ///
    /// Only one tutorial is ever active at a time (see <see cref="Start"/>'s remarks) - a second
    /// tutorial requested while one is already running is rejected outright, never queued, matching
    /// <see cref="GameFlow.IGameFlowService"/>'s "one active level flow" policy.
    ///
    /// Commands never throw for an invalid or currently-disallowed request - every one returns a
    /// result describing what happened instead (see <see cref="TutorialStartResult"/>/
    /// <see cref="TutorialCommandResult"/>).
    /// </summary>
    public interface ITutorialService : IGameService
    {
        TutorialState State { get; }

        /// <summary>The tutorial currently running/paused/finishing, or an invalid
        /// <see cref="TutorialId"/> while <see cref="TutorialState.Inactive"/>.</summary>
        TutorialId ActiveTutorialId { get; }

        /// <summary>The active step's id, or null if no tutorial is active.</summary>
        string ActiveStepId { get; }

        /// <summary>0-based index of the active step, or -1 if no tutorial is active.</summary>
        int ActiveStepIndex { get; }

        /// <summary>Total step count of the active tutorial, or 0 if none is active.</summary>
        int ActiveStepCount { get; }

        /// <summary>Raised after every <see cref="TutorialState"/> transition, as (previous, current) -
        /// offered directly for code that already holds this service, in addition to the specific
        /// events (<see cref="TutorialStartedEvent"/>, ...) published through
        /// <see cref="Runtime.Events.IEventService"/>.</summary>
        event Action<TutorialState, TutorialState> StateChanged;

        /// <summary>
        /// Registers a tutorial's content: its authoring metadata (<paramref name="definition"/>)
        /// and its ordered step sequence (<paramref name="steps"/>, composed in code - see
        /// <see cref="TutorialStepEntry"/>'s remarks). Call once per tutorial at composition-root
        /// time, after this service's own <see cref="Runtime.Services.IGameService.Initialize"/> -
        /// like <c>UnlockService</c>/<c>RewardService</c>/<c>QuestService</c>, persisted state is
        /// staged during <see cref="Load"/> and applied here, so registering after loading never
        /// loses a previously-completed tutorial's saved state.
        ///
        /// <paramref name="inputContext"/>, if supplied, is pushed onto <see cref="IInputService"/>'s
        /// context stack for the entire duration of a run (popped exactly once when it ends) - the
        /// tutorial's input-gating policy (e.g. "only allow Move and Confirm while this tutorial is
        /// active"). Left null, this tutorial does not touch the input context stack at all.
        ///
        /// Throws <see cref="ArgumentException"/> if <paramref name="definition"/> has no Id, and
        /// <see cref="InvalidOperationException"/> for a duplicate Id - both are authoring/
        /// programmer errors caught once at startup, not a normal runtime outcome.
        /// </summary>
        void RegisterTutorial(TutorialDefinition definition, IReadOnlyList<TutorialStepEntry> steps, InputContextDefinition? inputContext = null);

        bool IsRegistered(TutorialId id);

        /// <summary>True once this tutorial has reached a terminal Completed outcome - via a normal
        /// finish or a skip (see <see cref="WasSkipped"/>) both count. Never true for a cancelled
        /// run - see <see cref="Cancel"/>'s remarks.</summary>
        bool HasCompleted(TutorialId id);

        /// <summary>True only if this tutorial's most recent completion was via <see cref="Skip"/>.</summary>
        bool WasSkipped(TutorialId id);

        /// <summary>True if every entry in <see cref="TutorialDefinition.Prerequisites"/> has
        /// completed (see <see cref="HasCompleted"/>). Vacuously true for an empty prerequisite
        /// list. False (with a logged warning) for a prerequisite id that was never registered.</summary>
        bool ArePrerequisitesSatisfied(TutorialId id);

        /// <summary>The registered authoring data for <paramref name="id"/>, or null if it was
        /// never registered - lets a UI layer resolve display/localization keys without the
        /// tutorial being active.</summary>
        TutorialDefinition GetDefinition(TutorialId id);

        /// <summary>
        /// Starts <paramref name="id"/>: acquires gameplay pause/input gating if configured, and
        /// begins its first step (or the persisted resume step - see
        /// <see cref="TutorialPersistencePolicy.ResumeProgress"/>). Rejected outright
        /// (<see cref="TutorialStartResult.Blocked"/>) if a *different* tutorial is already active -
        /// this service never queues a second request.
        /// </summary>
        TutorialStartResult Start(TutorialId id);

        /// <summary>Ends the active tutorial early as a deliberate "I already know this" outcome -
        /// publishes <see cref="TutorialSkippedEvent"/> instead of <see cref="TutorialCompletedEvent"/>,
        /// but otherwise marks it completed identically (so <see cref="TutorialRepeatPolicy.Once"/>
        /// still blocks a later replay). Rejected (<see cref="TutorialCommandResult.NotSkippable"/>)
        /// if the active tutorial's <see cref="TutorialSkipPolicy"/> is <see cref="TutorialSkipPolicy.NotSkippable"/>.</summary>
        TutorialCommandResult Skip();

        /// <summary>
        /// Aborts the active tutorial - publishes <see cref="TutorialCancelledEvent"/> and, unlike
        /// <see cref="Skip"/>/a normal finish, does <b>not</b> mark it completed: any persisted
        /// resume point is discarded, and the next <see cref="Start"/> for this id begins fresh at
        /// its first step regardless of <see cref="TutorialRepeatPolicy"/>. Always releases any
        /// gameplay pause/input gating this run acquired.
        /// </summary>
        TutorialCommandResult Cancel();

        /// <summary>
        /// Force-starts <paramref name="id"/> from its first step, bypassing
        /// <see cref="TutorialRepeatPolicy"/> entirely (development/testing/"replay tutorial" UI
        /// affordance - the same "explicit override" spirit as
        /// <see cref="Quests.Quests.IQuestService.ForceReset"/>). If <paramref name="id"/> is
        /// currently the active tutorial, its in-progress run is cancelled first (no
        /// <see cref="TutorialCancelledEvent"/> is published for that - only
        /// <see cref="TutorialRestartedEvent"/>, followed by the new run's own
        /// <see cref="TutorialStartedEvent"/>). Rejected (<see cref="TutorialCommandResult.Blocked"/>)
        /// if a *different* tutorial is currently active.
        /// </summary>
        TutorialCommandResult Restart(TutorialId id);

        /// <summary>
        /// Force-completes the active step regardless of its type - the framework's
        /// "Manual/External Completion" mechanism (see CLAUDE.md's Phase 9 brief, section 9): a
        /// player acknowledging an <see cref="Steps.InstructionStep"/>, or arbitrary game code
        /// deciding some condition it doesn't want to express as an <see cref="Conditions.ITutorialCondition"/>
        /// was met. A no-op (<see cref="TutorialCommandResult.InvalidState"/>) if the active step
        /// already finished through its own trigger in the same frame - never double-advances.
        /// </summary>
        TutorialCommandResult CompleteCurrentStep();

        /// <summary>Persists every registered tutorial's state per its own
        /// <see cref="TutorialPersistencePolicy"/>. Never called automatically except once, from
        /// <see cref="Runtime.Services.IGameService.Shutdown"/>, and only if something changed since
        /// the last Save/Load - the same dirty-flag save policy every persisted service in this
        /// framework uses.</summary>
        void Save();

        /// <summary>Reloads persisted state. Safe to call after tutorials are already registered
        /// (re-applies to each); called automatically once during <see cref="Runtime.Services.IGameService.Initialize"/>.</summary>
        void Load();

        /// <summary>Clears every tutorial's runtime state (not its content) without touching saved
        /// data on disk until <see cref="Save"/> is called. Cancels the active run first, if any.
        /// Development/testing use.</summary>
        void ResetToDefaults();
    }
}
