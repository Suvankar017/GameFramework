using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Input;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Tutorials.Steps;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Default <see cref="ITutorialService"/>.
    ///
    /// <para><b>Re-entrancy.</b> Every public command wraps its state transition and every event it
    /// publishes in a single <see cref="_commandInProgress"/> guard - the same pattern
    /// <see cref="GameFlow.GameFlowService"/> established - so a listener that calls another command
    /// from inside, say, a <see cref="TutorialCompletedEvent"/> handler gets
    /// <see cref="TutorialCommandResult.Blocked"/> back rather than corrupting the in-progress
    /// transition. A step completing "on its own" (an input/event/condition/duration trigger firing
    /// from <see cref="Tick"/> or an <see cref="Runtime.Events.IEventService"/> callback) funnels
    /// through the same guard via <see cref="OnActiveStepCompleted"/>.</para>
    ///
    /// <para><b>Pause</b> is fully delegated to <see cref="ITimeService"/>'s existing
    /// reference-counted Pause/Resume - no new pause abstraction is introduced (see
    /// CLAUDE.md's Phase 9 brief, section 17). Unlike <see cref="GameFlow.IGameFlowService.PauseGameplay"/>,
    /// this service never hands a pause token to external callers, so it does not need
    /// <see cref="GameFlow.IPauseToken"/>'s bookkeeping - it acquires at most one pause of its own
    /// (<see cref="_hasPausedGameplay"/>) and releases it exactly once. Separately,
    /// <see cref="TutorialState"/> reactively mirrors <see cref="ITimeService.IsPaused"/> every tick
    /// (<see cref="PollPauseState"/>), exactly like <see cref="GameFlow.GameFlowService"/>'s own
    /// reactive pause handling - so a tutorial correctly suspends its own step progression if
    /// *anything* pauses gameplay (a pause menu, <see cref="GameFlow.IGameFlowService.PauseGameplay"/>,
    /// or this tutorial's own <see cref="TutorialPausePolicy.PausesGameplay"/> acquisition, which
    /// pre-seeds <see cref="_wasTimePaused"/> specifically so it does not immediately observe its own
    /// pause as an external one).</para>
    ///
    /// <para><b>Not duplicated here.</b> This assembly references only
    /// <see cref="GameFramework.Core"/>/<see cref="GameFramework.Runtime"/>/<see cref="GameFramework.Input"/> -
    /// never Gameplay/GameFlow/Progression/Unlocks/Rewards/Quests/UI/Localization/Audio/Feedback.
    /// A tutorial reacts to generic <see cref="Runtime.Events.IEventService"/> events and
    /// <see cref="IInputService"/> logical actions only; what a step's event/condition actually
    /// means, and what a game does in response to <see cref="TutorialStepStartedEvent"/> (show UI,
    /// play audio, highlight an object), is entirely the game's decision.</para>
    /// </summary>
    public sealed class TutorialService : ITutorialService, IUpdatableService
    {
        private const string LogCategory = "Tutorials";
        private const string SaveKey = "GameFramework.Tutorials";
        private const int SaveVersion = 1;

        private sealed class TutorialRuntime
        {
            public TutorialDefinition Definition;
            public IReadOnlyList<TutorialStepEntry> Steps;
            public InputContextDefinition? InputContext;
            public bool HasStarted;
            public bool HasCompleted;
            public bool WasSkipped;

            /// <summary>Consumed by the next <see cref="Start"/> call, then reset to -1 - the
            /// cross-session resume point restored from persisted state (see
            /// <see cref="TutorialPersistencePolicy.ResumeProgress"/>).</summary>
            public int PendingResumeStepIndex = -1;

            /// <summary>The in-progress step index snapshotted for <see cref="Save"/> while this
            /// tutorial's policy is <see cref="TutorialPersistencePolicy.ResumeProgress"/> and it is
            /// currently running. -1 when not applicable.</summary>
            public int ResumeStepIndex = -1;
        }

        private readonly struct PersistedTutorialState
        {
            public readonly bool Completed;
            public readonly bool Skipped;
            public readonly int ResumeStepIndex;

            public PersistedTutorialState(bool completed, bool skipped, int resumeStepIndex)
            {
                Completed = completed;
                Skipped = skipped;
                ResumeStepIndex = resumeStepIndex;
            }
        }

        [Serializable]
        private sealed class TutorialSaveData
        {
            public List<string> TutorialIds = new List<string>();
            public List<int> Completed = new List<int>();
            public List<int> Skipped = new List<int>();
            public List<int> ResumeStepIndices = new List<int>();
        }

        private readonly TutorialLifecycleStateMachine _stateMachine = new TutorialLifecycleStateMachine();
        private readonly Dictionary<TutorialId, TutorialRuntime> _tutorials = new Dictionary<TutorialId, TutorialRuntime>();
        private readonly Dictionary<TutorialId, PersistedTutorialState> _pendingRestore = new Dictionary<TutorialId, PersistedTutorialState>();
        private readonly HashSet<TutorialId> _completedThisSession = new HashSet<TutorialId>();

        private ITimeService _time;
        private IEventService _events;
        private IPersistenceService _persistence;
        private IInputService _input;
        private ILoggingService _log;

        private TutorialId _activeId;
        private TutorialRuntime _activeRuntime;
        private ITutorialStep _activeStep;
        private int _activeStepIndex = -1;

        private bool _commandInProgress;
        private bool _hasPausedGameplay;
        private bool _hasPushedInputContext;
        private bool _wasTimePaused;
        private bool _isDirty;

        public TutorialState State => _stateMachine.Current;
        public TutorialId ActiveTutorialId => _activeId;
        public string ActiveStepId => _activeStep?.Id;
        public int ActiveStepIndex => _activeStepIndex;
        public int ActiveStepCount => _activeRuntime?.Steps.Count ?? 0;

        public event Action<TutorialState, TutorialState> StateChanged;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            _events = registry.Get<IEventService>();
            _persistence = registry.Get<IPersistenceService>();
            registry.TryGet(out _input);
            registry.TryGet(out _log);

            _stateMachine.StateChanged += OnStateMachineChanged;

            Load();
        }

        public void Shutdown()
        {
            _stateMachine.StateChanged -= OnStateMachineChanged;

            if (State != TutorialState.Inactive)
            {
                // Abrupt teardown mid-tutorial (application quit, bootstrap shutdown while a
                // tutorial is running) - release pause/input gating so nothing is left permanently
                // blocked, and force the state machine back to Inactive so a second Shutdown() call
                // (or a stray State read afterward) is well-defined - without publishing events into
                // services that may already be shutting down themselves.
                _activeStep?.Cancel();
                ReleaseActiveResources();

                if (State == TutorialState.Running || State == TutorialState.Paused)
                {
                    _stateMachine.TryTransition(TutorialState.Cancelling);
                    _stateMachine.TryTransition(TutorialState.Cancelled);
                }

                _stateMachine.TryTransition(TutorialState.Inactive);
                _activeId = default;
                _activeRuntime = null;
            }

            if (_isDirty)
            {
                Save();
            }
        }

        public void Tick()
        {
            if (_activeRuntime == null)
            {
                return;
            }

            PollPauseState();

            if (State == TutorialState.Running)
            {
                _activeStep?.Tick(_time.UnscaledDeltaTime);
            }
        }

        public void RegisterTutorial(TutorialDefinition definition, IReadOnlyList<TutorialStepEntry> steps, InputContextDefinition? inputContext = null)
        {
            Guard.NotNull(definition, nameof(definition));
            Guard.NotNull(steps, nameof(steps));

            TutorialId id = definition.Id;
            if (!id.IsValid)
            {
                throw new ArgumentException("TutorialDefinition has no Id assigned.", nameof(definition));
            }

            if (_tutorials.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate tutorial id '{id}'.");
            }

            var runtime = new TutorialRuntime
            {
                Definition = definition,
                Steps = steps,
                InputContext = inputContext
            };

            _tutorials.Add(id, runtime);
            ApplyPendingRestore(id, runtime);
        }

        public bool IsRegistered(TutorialId id) => _tutorials.ContainsKey(id);
        public bool HasCompleted(TutorialId id) => _tutorials.TryGetValue(id, out TutorialRuntime runtime) && runtime.HasCompleted;
        public bool WasSkipped(TutorialId id) => _tutorials.TryGetValue(id, out TutorialRuntime runtime) && runtime.WasSkipped;
        public TutorialDefinition GetDefinition(TutorialId id) => _tutorials.TryGetValue(id, out TutorialRuntime runtime) ? runtime.Definition : null;

        public bool ArePrerequisitesSatisfied(TutorialId id) =>
            _tutorials.TryGetValue(id, out TutorialRuntime runtime) && ArePrerequisitesSatisfiedInternal(runtime);

        public TutorialStartResult Start(TutorialId id)
        {
            if (_commandInProgress)
            {
                return TutorialStartResult.Blocked;
            }

            if (!_tutorials.TryGetValue(id, out TutorialRuntime runtime))
            {
                return TutorialStartResult.NotFound;
            }

            if (State != TutorialState.Inactive)
            {
                return _activeId == id ? TutorialStartResult.AlreadyRunning : TutorialStartResult.Blocked;
            }

            if (runtime.HasCompleted)
            {
                switch (runtime.Definition.RepeatPolicy)
                {
                    case TutorialRepeatPolicy.Once:
                        return TutorialStartResult.AlreadyCompleted;
                    case TutorialRepeatPolicy.OncePerSession:
                        if (_completedThisSession.Contains(id))
                        {
                            return TutorialStartResult.AlreadyCompleted;
                        }
                        break;
                }
            }

            if (!ArePrerequisitesSatisfiedInternal(runtime))
            {
                return TutorialStartResult.PrerequisiteNotMet;
            }

            _commandInProgress = true;
            try
            {
                int startStepIndex = 0;
                if (runtime.PendingResumeStepIndex >= 0 && runtime.PendingResumeStepIndex < runtime.Steps.Count)
                {
                    startStepIndex = runtime.PendingResumeStepIndex;
                }
                runtime.PendingResumeStepIndex = -1;

                _stateMachine.TryTransition(TutorialState.Starting);
                BeginRun(id, runtime, startStepIndex);

                return TutorialStartResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TutorialCommandResult Skip()
        {
            if (_commandInProgress)
            {
                return TutorialCommandResult.Blocked;
            }

            if (State != TutorialState.Running && State != TutorialState.Paused)
            {
                return TutorialCommandResult.NoActiveTutorial;
            }

            if (_activeRuntime.Definition.SkipPolicy == TutorialSkipPolicy.NotSkippable)
            {
                return TutorialCommandResult.NotSkippable;
            }

            _commandInProgress = true;
            try
            {
                DetachActiveStep();
                _activeStep?.Cancel();
                FinishInternal(skipped: true);
                return TutorialCommandResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TutorialCommandResult Cancel()
        {
            if (_commandInProgress)
            {
                return TutorialCommandResult.Blocked;
            }

            if (State != TutorialState.Running && State != TutorialState.Paused)
            {
                return TutorialCommandResult.NoActiveTutorial;
            }

            _commandInProgress = true;
            try
            {
                _stateMachine.TryTransition(TutorialState.Cancelling);

                DetachActiveStep();
                _activeStep?.Cancel();

                TutorialId cancelledId = _activeId;

                // A cancelled run is a deliberate abort, not an interruption - discard any resume
                // point regardless of persistence policy, so the next Start() begins fresh.
                _activeRuntime.HasStarted = false;
                _activeRuntime.PendingResumeStepIndex = -1;
                _activeRuntime.ResumeStepIndex = -1;
                _isDirty = true;

                ReleaseActiveResources();

                _stateMachine.TryTransition(TutorialState.Cancelled);
                _events.Publish(new TutorialCancelledEvent(cancelledId));

                ReturnToInactive();

                return TutorialCommandResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TutorialCommandResult Restart(TutorialId id)
        {
            if (_commandInProgress)
            {
                return TutorialCommandResult.Blocked;
            }

            if (!_tutorials.TryGetValue(id, out TutorialRuntime runtime))
            {
                return TutorialCommandResult.NotFound;
            }

            bool isCurrentlyActive = State != TutorialState.Inactive && _activeId == id;
            if (State != TutorialState.Inactive && !isCurrentlyActive)
            {
                return TutorialCommandResult.Blocked;
            }

            _commandInProgress = true;
            try
            {
                if (isCurrentlyActive)
                {
                    _stateMachine.TryTransition(TutorialState.Cancelling);
                    DetachActiveStep();
                    _activeStep?.Cancel();
                    ReleaseActiveResources();
                    _stateMachine.TryTransition(TutorialState.Cancelled);
                }

                runtime.HasStarted = false;
                runtime.HasCompleted = false;
                runtime.WasSkipped = false;
                runtime.PendingResumeStepIndex = -1;
                runtime.ResumeStepIndex = -1;
                _completedThisSession.Remove(id);
                _isDirty = true;

                _events.Publish(new TutorialRestartedEvent(id));

                _stateMachine.TryTransition(TutorialState.Starting);
                BeginRun(id, runtime, 0);

                return TutorialCommandResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TutorialCommandResult CompleteCurrentStep()
        {
            if (_commandInProgress)
            {
                return TutorialCommandResult.Blocked;
            }

            if (State != TutorialState.Running && State != TutorialState.Paused)
            {
                return TutorialCommandResult.NoActiveTutorial;
            }

            if (_activeStep == null || _activeStep.State != TutorialStepState.Active)
            {
                return TutorialCommandResult.InvalidState;
            }

            // Peek only - Complete() synchronously raises Completed, which OnActiveStepCompleted
            // picks up and advances under its own guard. Holding _commandInProgress across this
            // call would make OnActiveStepCompleted see a false "already in progress" and drop it.
            _activeStep.Complete();
            return TutorialCommandResult.Success;
        }

        public void Save()
        {
            var data = new TutorialSaveData();

            foreach (KeyValuePair<TutorialId, TutorialRuntime> pair in _tutorials)
            {
                TutorialRuntime runtime = pair.Value;
                if (runtime.Definition.PersistencePolicy == TutorialPersistencePolicy.None)
                {
                    continue;
                }

                bool trackResume = runtime.Definition.PersistencePolicy == TutorialPersistencePolicy.ResumeProgress
                    && runtime.HasStarted && !runtime.HasCompleted;

                if (!runtime.HasCompleted && !trackResume)
                {
                    continue;
                }

                data.TutorialIds.Add(pair.Key.Value);
                data.Completed.Add(runtime.HasCompleted ? 1 : 0);
                data.Skipped.Add(runtime.WasSkipped ? 1 : 0);
                data.ResumeStepIndices.Add(trackResume ? runtime.ResumeStepIndex : -1);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            _pendingRestore.Clear();

            TutorialSaveData data = _persistence.Load(SaveKey, SaveVersion, new TutorialSaveData());
            int count = data.TutorialIds.Count;
            if (data.Completed.Count < count || data.Skipped.Count < count || data.ResumeStepIndices.Count < count)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "Persisted tutorial data has mismatched column lengths; ignoring stored state.");
                count = 0;
            }

            for (int i = 0; i < count; i++)
            {
                var id = new TutorialId(data.TutorialIds[i]);
                if (!id.IsValid)
                {
                    continue;
                }

                _pendingRestore[id] = new PersistedTutorialState(
                    data.Completed[i] != 0, data.Skipped[i] != 0, data.ResumeStepIndices[i]);
            }

            _isDirty = false;

            // RegisterTutorial normally runs after Load (see this class's remarks); this only
            // matters for an explicit re-Load() call after tutorials already exist.
            foreach (KeyValuePair<TutorialId, TutorialRuntime> pair in _tutorials)
            {
                ApplyPendingRestore(pair.Key, pair.Value);
            }
        }

        public void ResetToDefaults()
        {
            if (State != TutorialState.Inactive)
            {
                Cancel();
            }

            foreach (KeyValuePair<TutorialId, TutorialRuntime> pair in _tutorials)
            {
                pair.Value.HasStarted = false;
                pair.Value.HasCompleted = false;
                pair.Value.WasSkipped = false;
                pair.Value.PendingResumeStepIndex = -1;
                pair.Value.ResumeStepIndex = -1;
            }

            _completedThisSession.Clear();
            _pendingRestore.Clear();
            _isDirty = true;
        }

        private bool ArePrerequisitesSatisfiedInternal(TutorialRuntime runtime)
        {
            IReadOnlyList<TutorialId> prerequisites = runtime.Definition.Prerequisites;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                TutorialId prerequisiteId = prerequisites[i];
                if (!_tutorials.TryGetValue(prerequisiteId, out TutorialRuntime prerequisiteRuntime))
                {
                    _log?.Log(LogLevel.Warning, LogCategory, $"Prerequisite '{prerequisiteId}' is not a registered tutorial.");
                    return false;
                }

                if (!prerequisiteRuntime.HasCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyPendingRestore(TutorialId id, TutorialRuntime runtime)
        {
            if (!_pendingRestore.TryGetValue(id, out PersistedTutorialState persisted))
            {
                return;
            }

            runtime.HasCompleted = persisted.Completed;
            runtime.WasSkipped = persisted.Skipped;

            if (!persisted.Completed && persisted.ResumeStepIndex >= 0 && persisted.ResumeStepIndex < runtime.Steps.Count)
            {
                runtime.PendingResumeStepIndex = persisted.ResumeStepIndex;
            }

            _pendingRestore.Remove(id);
        }

        private void BeginRun(TutorialId id, TutorialRuntime runtime, int startStepIndex)
        {
            runtime.HasStarted = true;

            _activeId = id;
            _activeRuntime = runtime;
            _activeStep = null;
            _activeStepIndex = -1;

            // Step *instances* are registered once and reused across every run of this tutorial
            // (a Repeatable replay, Restart, or Cancel-then-Start) - without this, a step that
            // already reached Completed/Cancelled in a previous run would silently refuse to Begin
            // again (see ITutorialStep.Reset's remarks). Resetting every step unconditionally,
            // including ones before startStepIndex, is harmless: a resume run never revisits them.
            IReadOnlyList<TutorialStepEntry> allSteps = runtime.Steps;
            for (int i = 0; i < allSteps.Count; i++)
            {
                allSteps[i].Step.Reset();
            }

            if (runtime.Definition.PausePolicy == TutorialPausePolicy.PausesGameplay)
            {
                _time.Pause();
                _hasPausedGameplay = true;
                _log?.Log(LogLevel.Info, LogCategory, $"Tutorial '{id}' paused gameplay (reason: {runtime.Definition.PauseReason}).");
            }

            if (runtime.InputContext.HasValue && _input != null)
            {
                _input.PushContext(runtime.InputContext.Value);
                _hasPushedInputContext = true;
            }

            _stateMachine.TryTransition(TutorialState.Running);

            // Pre-seed the pause baseline *after* any self-acquired pause above, so PollPauseState's
            // next tick does not mistake our own PausesGameplay acquisition for an external pause
            // and immediately flip this run to Paused.
            _wasTimePaused = _time.IsPaused;

            _events.Publish(new TutorialStartedEvent(id));

            BeginStep(startStepIndex);
        }

        private void BeginStep(int stepIndex)
        {
            TutorialRuntime runtime = _activeRuntime;

            if (stepIndex >= runtime.Steps.Count)
            {
                FinishInternal(skipped: false);
                return;
            }

            _activeStepIndex = stepIndex;
            _activeStep = runtime.Steps[stepIndex].Step;
            _activeStep.Completed += OnActiveStepCompleted;

            if (runtime.Definition.PersistencePolicy == TutorialPersistencePolicy.ResumeProgress)
            {
                runtime.ResumeStepIndex = stepIndex;
                _isDirty = true;
            }

            _events.Publish(new TutorialStepStartedEvent(_activeId, _activeStep.Id, stepIndex, runtime.Steps.Count));

            _activeStep.Begin();

            // Begin() may have completed the step synchronously (e.g. a zero-duration WaitStep, or
            // a ConditionStep already satisfied). The Completed event it raised was already
            // subscribed above but is dropped by OnActiveStepCompleted's re-entrancy guard (we are
            // still inside this command's own guarded call stack) - drive the advance directly
            // instead, now that TutorialStepStartedEvent has already been published in order.
            if (_activeStep.State == TutorialStepState.Completed)
            {
                AdvanceCore();
            }
        }

        private void OnActiveStepCompleted()
        {
            if (_commandInProgress)
            {
                _log?.Log(LogLevel.Warning, LogCategory,
                    $"Tutorial '{_activeId}' step '{_activeStep?.Id}' completed while another command was already in progress; ignored.");
                return;
            }

            _commandInProgress = true;
            try
            {
                AdvanceCore();
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        private void AdvanceCore()
        {
            ITutorialStep completedStep = _activeStep;
            int completedIndex = _activeStepIndex;

            completedStep.Completed -= OnActiveStepCompleted;

            _events.Publish(new TutorialStepCompletedEvent(_activeId, completedStep.Id, completedIndex));

            int nextIndex = completedIndex + 1;
            if (nextIndex >= _activeRuntime.Steps.Count)
            {
                FinishInternal(skipped: false);
            }
            else
            {
                BeginStep(nextIndex);
            }
        }

        private void FinishInternal(bool skipped)
        {
            _stateMachine.TryTransition(TutorialState.Completing);

            TutorialId finishedId = _activeId;
            TutorialRuntime runtime = _activeRuntime;

            runtime.HasCompleted = true;
            runtime.WasSkipped = skipped;
            runtime.PendingResumeStepIndex = -1;
            runtime.ResumeStepIndex = -1;
            _completedThisSession.Add(finishedId);
            _isDirty = true;

            ReleaseActiveResources();

            _stateMachine.TryTransition(TutorialState.Completed);

            if (skipped)
            {
                _events.Publish(new TutorialSkippedEvent(finishedId));
            }
            else
            {
                _events.Publish(new TutorialCompletedEvent(finishedId));
            }

            ReturnToInactive();
        }

        private void DetachActiveStep()
        {
            if (_activeStep != null)
            {
                _activeStep.Completed -= OnActiveStepCompleted;
            }
        }

        private void ReleaseActiveResources()
        {
            if (_hasPausedGameplay)
            {
                _time.Resume();
                _hasPausedGameplay = false;
            }

            if (_hasPushedInputContext && _input != null)
            {
                _input.PopContext();
                _hasPushedInputContext = false;
            }

            _activeStep = null;
            _activeStepIndex = -1;
        }

        private void ReturnToInactive()
        {
            _stateMachine.TryTransition(TutorialState.Inactive);
            _activeId = default;
            _activeRuntime = null;
        }

        private void PollPauseState()
        {
            bool isPausedNow = _time.IsPaused;
            if (isPausedNow == _wasTimePaused)
            {
                return;
            }

            _wasTimePaused = isPausedNow;

            if (isPausedNow && State == TutorialState.Running)
            {
                if (_stateMachine.TryTransition(TutorialState.Paused))
                {
                    _events.Publish(new TutorialPausedEvent(_activeId));
                }
            }
            else if (!isPausedNow && State == TutorialState.Paused)
            {
                if (_stateMachine.TryTransition(TutorialState.Running))
                {
                    _events.Publish(new TutorialResumedEvent(_activeId));
                }
            }
        }

        private void OnStateMachineChanged(TutorialState previous, TutorialState current)
        {
            StateChanged?.Invoke(previous, current);
        }
    }
}
