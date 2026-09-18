using System;
using System.Collections.Generic;
using GameFramework.GameFlow.Checkpoints;
using GameFramework.GameFlow.Session;
using GameFramework.Gameplay;
using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Default <see cref="IGameFlowService"/>.
    ///
    /// <para><b>Scene loading</b> goes through <see cref="ISceneService"/>, and completion is
    /// detected via its <see cref="ISceneService.SceneLoaded"/> event rather than polling the
    /// <see cref="System.Object"/> <c>AsyncOperation</c> <see cref="ISceneService.LoadAsync"/>
    /// returns - event-driven completion is what lets this stay fully unit-testable against a fake
    /// <see cref="ISceneService"/> (an <c>AsyncOperation</c> cannot be constructed or driven
    /// manually outside of a real Unity scene load). A load cancelled mid-flight (see
    /// <see cref="ExitLevel"/>) simply stops this service listening for it - Unity itself does not
    /// support aborting an in-flight scene load (see <see cref="ISceneService"/>'s own remarks), so
    /// the scene may still finish loading in the background; this service's own state never reflects
    /// that stale completion, which is the guarantee this framework can actually make.</para>
    ///
    /// <para><b>Re-entrancy.</b> Every public command wraps its body in a single
    /// <see cref="_commandInProgress"/> guard spanning the state transition *and* every event it
    /// publishes - so a listener that calls another command from inside, say, a
    /// <see cref="LevelCompletedEvent"/> handler gets <see cref="TransitionResult.TransitionBlocked"/>/
    /// <see cref="RespawnResult.Blocked"/> rather than corrupting the in-progress transition.</para>
    ///
    /// <para><b>Pause</b> is fully delegated to <see cref="ITimeService"/>'s existing
    /// reference-counted Pause/Resume - this service tracks no pause flag of its own.
    /// <see cref="LevelFlowState"/> reactively mirrors <see cref="ITimeService.IsPaused"/> every
    /// tick (mirroring <see cref="GameFramework.Gameplay.GameplayService"/>'s own reactive pause
    /// handling), so *any* pause source - a <see cref="PauseGameplay"/> token, the Phase 5
    /// application-focus handler backgrounding the app, or a totally unrelated system calling
    /// <see cref="ITimeService.Pause"/> directly - is reflected correctly with no special-casing.</para>
    ///
    /// <para><b>Not duplicated here.</b> Progression/statistics/objectives/quests/rewards (Phase
    /// 6/7) are never called into directly - <see cref="LevelCompletedEvent"/>/
    /// <see cref="LevelFailedEvent"/>/<see cref="GameplaySessionCompletedEvent"/> carry everything a
    /// listener needs to react (attempt number, elapsed time, result) without this assembly
    /// referencing Progression/Unlocks/Rewards/Quests at all.</para>
    /// </summary>
    public sealed class GameFlowService : IGameFlowService, IUpdatableService
    {
        private const string LogCategory = "GameFlow";
        private const string CheckpointSaveKey = "GameFramework.GameFlow.Checkpoint";
        private const int CheckpointSaveVersion = 1;

        [Serializable]
        private sealed class CheckpointSaveData
        {
            public string LevelId;
            public string CheckpointId;
            public bool HasTransform;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private readonly LevelFlowStateMachine _stateMachine = new LevelFlowStateMachine();
        private readonly Dictionary<LevelId, int> _attemptCounts = new Dictionary<LevelId, int>();
        private readonly HashSet<PauseToken> _activePauseTokens = new HashSet<PauseToken>();
        private readonly Dictionary<string, int> _pauseReasonCounts = new Dictionary<string, int>();

        private ITimeService _time;
        private IEventService _events;
        private ISceneService _scene;
        private IPersistenceService _persistence;
        private ILoggingService _log;
        private IGameplayService _gameplayService;

        private LevelDefinition _currentLevel;
        private GameplaySession _currentSession;
        private string _pendingSceneName;
        private string _pendingSessionMode;
        private bool _autoStartAfterLoad;
        private bool _commandInProgress;
        private bool _wasTimePaused;

        public LevelFlowState State => _stateMachine.Current;
        public IReadOnlyList<LevelFlowState> StateHistory => _stateMachine.History;
        public LevelDefinition CurrentLevel => _currentLevel;
        public GameplaySession CurrentSession => _currentSession;
        public bool IsPaused => _time.IsPaused;
        public IReadOnlyCollection<string> ActivePauseReasons => _pauseReasonCounts.Keys;
        public bool PersistCheckpoints { get; set; }

        public event Action<LevelFlowState, LevelFlowState> StateChanged;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            _events = registry.Get<IEventService>();
            _scene = registry.Get<ISceneService>();
            _persistence = registry.Get<IPersistenceService>();
            registry.TryGet(out _log);
            registry.TryGet(out _gameplayService);

            _stateMachine.StateChanged += OnStateMachineChanged;
            _scene.SceneLoaded += OnSceneLoaded;

            _wasTimePaused = _time.IsPaused;
        }

        public void Shutdown()
        {
            _scene.SceneLoaded -= OnSceneLoaded;
            _stateMachine.StateChanged -= OnStateMachineChanged;

            ReleaseAllPauseTokens();

            _currentSession?.Dispose();
            _currentSession = null;
            _currentLevel = null;
            _pendingSceneName = null;
        }

        public void Tick()
        {
            PollPauseState();

            if (_currentSession != null && State == LevelFlowState.Playing)
            {
                _currentSession.AccumulateTime(_time.ScaledDeltaTime);
            }
        }

        public LevelLoadResult LoadLevel(LevelDefinition level, string mode = null)
        {
            if (level == null || !level.Id.IsValid || string.IsNullOrEmpty(level.SceneName))
            {
                _log?.Log(LogLevel.Warning, LogCategory, "LoadLevel called with an invalid LevelDefinition; rejected.");
                return LevelLoadResult.InvalidLevel;
            }

            if (_commandInProgress || State != LevelFlowState.Unloaded)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"LoadLevel('{level.Id}') rejected - flow is not Unloaded (current: {State}).");
                return LevelLoadResult.Rejected;
            }

            _commandInProgress = true;
            try
            {
                _currentLevel = level;
                _pendingSessionMode = mode ?? level.Mode;

                _stateMachine.TryTransition(LevelFlowState.Loading);
                _events.Publish(new LevelLoadingStartedEvent(level.Id));

                _pendingSceneName = level.SceneName;
                _scene.LoadAsync(level.SceneName, SceneLoadMode.Single, activateOnLoad: true);

                return LevelLoadResult.Started;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TransitionResult StartLevel()
        {
            if (_commandInProgress)
            {
                return TransitionResult.TransitionBlocked;
            }

            _commandInProgress = true;
            try
            {
                return StartLevelInternal();
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TransitionResult CompleteLevel(GameplayResult result = default)
        {
            if (_commandInProgress)
            {
                return TransitionResult.TransitionBlocked;
            }

            if (State != LevelFlowState.Playing)
            {
                return TransitionResult.InvalidTransition;
            }

            if (result.IsUnspecified)
            {
                result = GameplayResult.Success();
            }

            _commandInProgress = true;
            try
            {
                _stateMachine.TryTransition(LevelFlowState.Completing);

                _gameplayService?.EndPlay();
                _currentSession.Complete(result);

                _events.Publish(new GameplaySessionCompletedEvent(
                    _currentSession.Id, _currentSession.LevelId, _currentSession.AttemptNumber,
                    _currentSession.ElapsedGameplayTime, result));

                _stateMachine.TryTransition(LevelFlowState.Completed);

                _events.Publish(new LevelCompletedEvent(
                    _currentSession.LevelId, _currentSession.Id, _currentSession.AttemptNumber,
                    _currentSession.ElapsedGameplayTime, result));

                return TransitionResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TransitionResult FailLevel(GameplayResult result = default)
        {
            if (_commandInProgress)
            {
                return TransitionResult.TransitionBlocked;
            }

            if (State != LevelFlowState.Playing)
            {
                return TransitionResult.InvalidTransition;
            }

            if (result.IsUnspecified)
            {
                result = GameplayResult.Failure();
            }

            _commandInProgress = true;
            try
            {
                _stateMachine.TryTransition(LevelFlowState.Failing);

                _gameplayService?.EndPlay();
                _currentSession.Fail(result);

                _events.Publish(new GameplaySessionFailedEvent(
                    _currentSession.Id, _currentSession.LevelId, _currentSession.AttemptNumber,
                    _currentSession.ElapsedGameplayTime, result));

                _stateMachine.TryTransition(LevelFlowState.Failed);

                _events.Publish(new LevelFailedEvent(
                    _currentSession.LevelId, _currentSession.Id, _currentSession.AttemptNumber, result));

                return TransitionResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TransitionResult Retry(bool reloadScene = true)
        {
            if (_commandInProgress)
            {
                return TransitionResult.TransitionBlocked;
            }

            _commandInProgress = true;
            try
            {
                TransitionResult transitionResult = _stateMachine.TryTransition(LevelFlowState.Restarting);
                if (transitionResult != TransitionResult.Success)
                {
                    return transitionResult;
                }

                if (_currentSession != null &&
                    (_currentSession.State == GameplaySessionState.Active || _currentSession.State == GameplaySessionState.Paused))
                {
                    _gameplayService?.EndPlay();
                    _currentSession.Abort(GameplayResult.Cancelled("Restarted"));
                }

                _currentSession?.Dispose();
                _currentSession = null;

                _events.Publish(new LevelRestartedEvent(_currentLevel.Id));

                if (reloadScene)
                {
                    _autoStartAfterLoad = true;
                    _pendingSceneName = _currentLevel.SceneName;

                    _stateMachine.TryTransition(LevelFlowState.Loading);
                    _events.Publish(new LevelLoadingStartedEvent(_currentLevel.Id));

                    _scene.LoadAsync(_currentLevel.SceneName, SceneLoadMode.Single, activateOnLoad: true);
                }
                else
                {
                    _stateMachine.TryTransition(LevelFlowState.Ready);
                    _events.Publish(new LevelReadyEvent(_currentLevel.Id));

                    StartLevelInternal();
                }

                return TransitionResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public RespawnResult Respawn()
        {
            if (_commandInProgress)
            {
                return RespawnResult.Blocked;
            }

            if (_currentSession == null)
            {
                return RespawnResult.NoActiveSession;
            }

            if (State != LevelFlowState.Failed && State != LevelFlowState.Playing)
            {
                return RespawnResult.InvalidState;
            }

            if (!_currentSession.Checkpoints.TryGetCurrent(out CheckpointRecord record))
            {
                return RespawnResult.NoCheckpoint;
            }

            _commandInProgress = true;
            try
            {
                if (State == LevelFlowState.Failed)
                {
                    TransitionResult transitionResult = _stateMachine.TryTransition(LevelFlowState.Playing);
                    if (transitionResult != TransitionResult.Success)
                    {
                        return RespawnResult.InvalidState;
                    }

                    _currentSession.Revive();
                    _gameplayService?.BeginPlay();
                }

                _currentSession.NotifyRespawn();

                Vector3 position = record.Transform?.Position ?? Vector3.zero;
                Quaternion rotation = record.Transform?.Rotation ?? Quaternion.identity;

                _events.Publish(new PlayerRespawnedEvent(
                    _currentSession.Id, record.Id, position, rotation, record.Transform.HasValue));

                return RespawnResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public TransitionResult ExitLevel()
        {
            if (_commandInProgress)
            {
                return TransitionResult.TransitionBlocked;
            }

            _commandInProgress = true;
            try
            {
                TransitionResult transitionResult = _stateMachine.TryTransition(LevelFlowState.Exiting);
                if (transitionResult != TransitionResult.Success)
                {
                    return transitionResult;
                }

                _pendingSceneName = null;
                _autoStartAfterLoad = false;

                if (_currentSession != null &&
                    (_currentSession.State == GameplaySessionState.Active || _currentSession.State == GameplaySessionState.Paused))
                {
                    _gameplayService?.EndPlay();
                    _currentSession.Abort(GameplayResult.Aborted("Exited"));
                }

                _currentSession?.Dispose();
                _currentSession = null;

                LevelId exitedLevelId = _currentLevel != null ? _currentLevel.Id : default;
                _currentLevel = null;

                _stateMachine.TryTransition(LevelFlowState.Unloaded);
                _events.Publish(new LevelExitedEvent(exitedLevelId));

                return TransitionResult.Success;
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        public IPauseToken PauseGameplay(string reason = null)
        {
            string effectiveReason = string.IsNullOrEmpty(reason) ? "Unspecified" : reason;
            var token = new PauseToken(effectiveReason, ReleasePauseToken);

            _activePauseTokens.Add(token);
            _pauseReasonCounts.TryGetValue(effectiveReason, out int count);
            _pauseReasonCounts[effectiveReason] = count + 1;

            _time.Pause();
            return token;
        }

        public bool RegisterCheckpoint(string id, CheckpointData? transform = null, IGameplaySnapshot snapshot = null)
        {
            if (_currentSession == null)
            {
                return false;
            }

            _currentSession.Checkpoints.Register(id, transform, snapshot);
            return true;
        }

        public bool ActivateCheckpoint(string id)
        {
            if (_currentSession == null || !_currentSession.Checkpoints.Activate(id))
            {
                return false;
            }

            _events.Publish(new CheckpointActivatedEvent(_currentSession.Id, id));

            if (PersistCheckpoints)
            {
                SaveCheckpointState();
            }

            return true;
        }

        public bool TryLoadPersistedCheckpoint(out string checkpointId, out CheckpointData? transform)
        {
            checkpointId = null;
            transform = null;

            if (_currentLevel == null || !_persistence.Exists(CheckpointSaveKey))
            {
                return false;
            }

            CheckpointSaveData data = _persistence.Load(CheckpointSaveKey, CheckpointSaveVersion, (CheckpointSaveData)null);
            if (data == null || !string.Equals(data.LevelId, _currentLevel.Id.ToString(), StringComparison.Ordinal))
            {
                return false;
            }

            checkpointId = data.CheckpointId;
            transform = data.HasTransform ? new CheckpointData { Id = data.CheckpointId, Position = data.Position, Rotation = data.Rotation } : (CheckpointData?)null;
            return true;
        }

        private TransitionResult StartLevelInternal()
        {
            TransitionResult transitionResult = _stateMachine.TryTransition(LevelFlowState.Playing);
            if (transitionResult != TransitionResult.Success)
            {
                return transitionResult;
            }

            LevelId levelId = _currentLevel.Id;
            _attemptCounts.TryGetValue(levelId, out int attempt);
            attempt++;
            _attemptCounts[levelId] = attempt;

            _currentSession = new GameplaySession(levelId, attempt, _pendingSessionMode, _time.Realtime);
            _currentSession.Start();
            _wasTimePaused = _time.IsPaused;

            _gameplayService?.BeginPlay();

            _events.Publish(new GameplaySessionStartedEvent(_currentSession.Id, levelId, attempt));
            _events.Publish(new LevelStartedEvent(levelId, _currentSession.Id, attempt));

            return TransitionResult.Success;
        }

        private void OnSceneLoaded(string sceneName)
        {
            if (_pendingSceneName == null || !string.Equals(sceneName, _pendingSceneName, StringComparison.Ordinal) || State != LevelFlowState.Loading)
            {
                return;
            }

            _pendingSceneName = null;

            _commandInProgress = true;
            try
            {
                _stateMachine.TryTransition(LevelFlowState.Initializing);
                _events.Publish(new LevelInitializingEvent(_currentLevel.Id));

                _stateMachine.TryTransition(LevelFlowState.Ready);
                _events.Publish(new LevelReadyEvent(_currentLevel.Id));

                if (_autoStartAfterLoad)
                {
                    _autoStartAfterLoad = false;
                    StartLevelInternal();
                }
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        private void PollPauseState()
        {
            if (_currentSession == null)
            {
                return;
            }

            bool isPausedNow = _time.IsPaused;
            if (isPausedNow == _wasTimePaused)
            {
                return;
            }

            _wasTimePaused = isPausedNow;

            if (isPausedNow && State == LevelFlowState.Playing)
            {
                if (_stateMachine.TryTransition(LevelFlowState.Paused) == TransitionResult.Success)
                {
                    _currentSession.Pause();
                    _events.Publish(new GameplaySessionPausedEvent(_currentSession.Id));
                }
            }
            else if (!isPausedNow && State == LevelFlowState.Paused)
            {
                if (_stateMachine.TryTransition(LevelFlowState.Playing) == TransitionResult.Success)
                {
                    _currentSession.Resume();
                    _events.Publish(new GameplaySessionResumedEvent(_currentSession.Id));
                }
            }
        }

        private void ReleasePauseToken(PauseToken token)
        {
            if (!_activePauseTokens.Remove(token))
            {
                return;
            }

            if (_pauseReasonCounts.TryGetValue(token.Reason, out int count))
            {
                if (count <= 1)
                {
                    _pauseReasonCounts.Remove(token.Reason);
                }
                else
                {
                    _pauseReasonCounts[token.Reason] = count - 1;
                }
            }

            _time.Resume();
        }

        private void ReleaseAllPauseTokens()
        {
            if (_activePauseTokens.Count == 0)
            {
                return;
            }

            var tokens = new List<PauseToken>(_activePauseTokens);
            foreach (PauseToken token in tokens)
            {
                token.Release();
            }
        }

        private void SaveCheckpointState()
        {
            if (_currentSession == null || !_currentSession.Checkpoints.TryGetCurrent(out CheckpointRecord record))
            {
                return;
            }

            var data = new CheckpointSaveData
            {
                LevelId = _currentSession.LevelId.ToString(),
                CheckpointId = record.Id,
                HasTransform = record.Transform.HasValue,
                Position = record.Transform?.Position ?? Vector3.zero,
                Rotation = record.Transform?.Rotation ?? Quaternion.identity
            };

            _persistence.Save(CheckpointSaveKey, data, CheckpointSaveVersion);
        }

        private void OnStateMachineChanged(LevelFlowState previous, LevelFlowState current)
        {
            StateChanged?.Invoke(previous, current);
            _events.Publish(new LevelFlowStateChangedEvent(previous, current));
        }
    }
}
