using System.Collections.Generic;
using GameFramework.GameFlow.Session;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.GameFlow.Tests
{
    public class GameFlowServiceTests
    {
        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private FakeSceneService _scene;
        private EventService _events;
        private PersistenceService _persistence;
        private GameFlowService _flow;
        private LevelDefinition _level;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _time, out _scene, out _events, out _persistence);
            _flow = new GameFlowService();
            _flow.Initialize(_registry);

            _level = TestDefinitions.Level("Level1", "Level1Scene");
        }

        [TearDown]
        public void TearDown()
        {
            _flow.Shutdown();
            Object.DestroyImmediate(_level);
        }

        private void LoadAndReachReady()
        {
            _flow.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");
        }

        [Test]
        public void InitialState_IsUnloaded_WithNoSession()
        {
            Assert.AreEqual(LevelFlowState.Unloaded, _flow.State);
            Assert.IsNull(_flow.CurrentSession);
            Assert.IsNull(_flow.CurrentLevel);
        }

        [Test]
        public void LoadLevel_WithInvalidDefinition_ReturnsInvalidLevel()
        {
            LevelLoadResult result = _flow.LoadLevel(null);

            Assert.AreEqual(LevelLoadResult.InvalidLevel, result);
            Assert.AreEqual(LevelFlowState.Unloaded, _flow.State);
        }

        [Test]
        public void LoadLevel_Valid_StartsLoadingAndRequestsScene()
        {
            LevelLoadResult result = _flow.LoadLevel(_level);

            Assert.AreEqual(LevelLoadResult.Started, result);
            Assert.AreEqual(LevelFlowState.Loading, _flow.State);
            Assert.AreEqual(1, _scene.LoadAsyncCallCount);
            Assert.AreEqual("Level1Scene", _scene.LastRequestedSceneName);
        }

        [Test]
        public void LoadLevel_WhileAlreadyLoading_IsRejected()
        {
            _flow.LoadLevel(_level);

            LevelLoadResult second = _flow.LoadLevel(_level);

            Assert.AreEqual(LevelLoadResult.Rejected, second);
            Assert.AreEqual(1, _scene.LoadAsyncCallCount);
        }

        [Test]
        public void SceneLoaded_ForPendingLevel_ReachesReady_AndPublishesPipelineEvents()
        {
            var received = new List<string>();
            _events.Subscribe<LevelLoadingStartedEvent>(e => received.Add("LoadingStarted"));
            _events.Subscribe<LevelInitializingEvent>(e => received.Add("Initializing"));
            _events.Subscribe<LevelReadyEvent>(e => received.Add("Ready"));

            LoadAndReachReady();

            Assert.AreEqual(LevelFlowState.Ready, _flow.State);
            CollectionAssert.AreEqual(new[] { "LoadingStarted", "Initializing", "Ready" }, received);
        }

        [Test]
        public void SceneLoaded_ForUnrelatedScene_IsIgnored()
        {
            _flow.LoadLevel(_level);

            _scene.RaiseSceneLoaded("SomeOtherScene");

            Assert.AreEqual(LevelFlowState.Loading, _flow.State);
        }

        [Test]
        public void StartLevel_FromReady_CreatesSessionAndTransitionsToPlaying()
        {
            LoadAndReachReady();

            TransitionResult result = _flow.StartLevel();

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.IsNotNull(_flow.CurrentSession);
            Assert.AreEqual(1, _flow.CurrentSession.AttemptNumber);
            Assert.AreEqual(GameplaySessionState.Active, _flow.CurrentSession.State);
        }

        [Test]
        public void StartLevel_FromWrongState_ReturnsInvalidTransition()
        {
            TransitionResult result = _flow.StartLevel();

            Assert.AreEqual(TransitionResult.InvalidTransition, result);
        }

        [Test]
        public void TimePause_ReflectsIntoPausedState_AndSessionPauseCount()
        {
            LoadAndReachReady();
            _flow.StartLevel();

            _time.Pause();
            _flow.Tick();

            Assert.AreEqual(LevelFlowState.Paused, _flow.State);
            Assert.AreEqual(1, _flow.CurrentSession.PauseCount);
            Assert.AreEqual(GameplaySessionState.Paused, _flow.CurrentSession.State);
        }

        [Test]
        public void TimeResume_AfterPause_ReturnsToPlaying()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _time.Pause();
            _flow.Tick();

            _time.Resume();
            _flow.Tick();

            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(GameplaySessionState.Active, _flow.CurrentSession.State);
        }

        [Test]
        public void ElapsedGameplayTime_AccumulatesOnlyWhilePlaying()
        {
            LoadAndReachReady();
            _flow.StartLevel();

            _time.ScaledDeltaTime = 0.5f;
            _flow.Tick();
            _flow.Tick();

            _time.Pause();
            _flow.Tick();
            _flow.Tick(); // paused now - should not accumulate

            Assert.AreEqual(1.0f, _flow.CurrentSession.ElapsedGameplayTime, 0.0001f);
        }

        [Test]
        public void PauseGameplay_ReturnsToken_AndReleasingItResumesTime()
        {
            IPauseToken token = _flow.PauseGameplay("Tutorial");

            Assert.IsTrue(_flow.IsPaused);
            Assert.Contains("Tutorial", new List<string>(_flow.ActivePauseReasons));

            token.Release();

            Assert.IsFalse(_flow.IsPaused);
        }

        [Test]
        public void PauseGameplay_TwoOwners_BothMustReleaseBeforeResuming()
        {
            IPauseToken tutorial = _flow.PauseGameplay("Tutorial");
            IPauseToken dialog = _flow.PauseGameplay("Dialog");

            tutorial.Release();
            Assert.IsTrue(_flow.IsPaused, "Should still be paused - Dialog's pause is still outstanding.");

            dialog.Release();
            Assert.IsFalse(_flow.IsPaused);
        }

        [Test]
        public void CompleteLevel_FromPlaying_TransitionsToCompleted_WithDefaultSuccessResult()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            GameplaySessionCompletedEvent? captured = null;
            _events.Subscribe<GameplaySessionCompletedEvent>(e => captured = e);

            TransitionResult result = _flow.CompleteLevel();

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Completed, _flow.State);
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(GameplayResultKind.Success, captured.Value.Result.Kind);
        }

        [Test]
        public void CompleteLevel_FromWrongState_ReturnsInvalidTransition()
        {
            TransitionResult result = _flow.CompleteLevel();

            Assert.AreEqual(TransitionResult.InvalidTransition, result);
        }

        [Test]
        public void FailLevel_FromPlaying_TransitionsToFailed_WithDefaultFailureResult()
        {
            LoadAndReachReady();
            _flow.StartLevel();

            TransitionResult result = _flow.FailLevel();

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Failed, _flow.State);
            Assert.AreEqual(GameplayResultKind.Failure, _flow.CurrentSession.Result.Kind);
        }

        [Test]
        public void Respawn_WithNoActiveSession_ReturnsNoActiveSession()
        {
            RespawnResult result = _flow.Respawn();

            Assert.AreEqual(RespawnResult.NoActiveSession, result);
        }

        [Test]
        public void Respawn_WithNoCheckpoint_ReturnsNoCheckpoint()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _flow.FailLevel();

            RespawnResult result = _flow.Respawn();

            Assert.AreEqual(RespawnResult.NoCheckpoint, result);
        }

        [Test]
        public void Respawn_AfterFailure_WithCheckpoint_ResumesSameAttempt()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            int firstAttempt = _flow.CurrentSession.AttemptNumber;
            _flow.RegisterCheckpoint("CP1");
            _flow.ActivateCheckpoint("CP1");
            _flow.FailLevel();

            PlayerRespawnedEvent? captured = null;
            _events.Subscribe<PlayerRespawnedEvent>(e => captured = e);

            RespawnResult result = _flow.Respawn();

            Assert.AreEqual(RespawnResult.Success, result);
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(firstAttempt, _flow.CurrentSession.AttemptNumber, "Respawn continues the same attempt.");
            Assert.AreEqual(1, _flow.CurrentSession.RespawnCount);
            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual("CP1", captured.Value.CheckpointId);
        }

        [Test]
        public void Respawn_WhilePlaying_WithCheckpoint_DoesNotChangeState()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _flow.RegisterCheckpoint("CP1");
            _flow.ActivateCheckpoint("CP1");

            RespawnResult result = _flow.Respawn();

            Assert.AreEqual(RespawnResult.Success, result);
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.CurrentSession.RespawnCount);
        }

        [Test]
        public void Retry_WithoutReload_StartsNewAttemptImmediately()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _flow.FailLevel();
            int loadCallCountBeforeRetry = _scene.LoadAsyncCallCount;

            TransitionResult result = _flow.Retry(reloadScene: false);

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(2, _flow.CurrentSession.AttemptNumber);
            Assert.AreEqual(loadCallCountBeforeRetry, _scene.LoadAsyncCallCount, "No reload was requested.");
        }

        [Test]
        public void Retry_WithReload_ReloadsSceneThenAutoStartsNewAttempt()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _flow.FailLevel();
            int loadCallCountBeforeRetry = _scene.LoadAsyncCallCount;

            TransitionResult result = _flow.Retry(reloadScene: true);

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Loading, _flow.State);
            Assert.AreEqual(loadCallCountBeforeRetry + 1, _scene.LoadAsyncCallCount);

            _scene.RaiseSceneLoaded("Level1Scene");

            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(2, _flow.CurrentSession.AttemptNumber);
        }

        [Test]
        public void ExitLevel_FromPlaying_AbortsSessionAndReturnsToUnloaded()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            LevelExitedEvent? captured = null;
            _events.Subscribe<LevelExitedEvent>(e => captured = e);

            TransitionResult result = _flow.ExitLevel();

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Unloaded, _flow.State);
            Assert.IsNull(_flow.CurrentSession);
            Assert.IsNull(_flow.CurrentLevel);
            Assert.IsTrue(captured.HasValue);
        }

        [Test]
        public void ExitLevel_WhileLoading_CancelsThePendingLoad()
        {
            _flow.LoadLevel(_level);

            TransitionResult result = _flow.ExitLevel();
            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Unloaded, _flow.State);

            // A stale completion for the cancelled load must not revive the flow.
            _scene.RaiseSceneLoaded("Level1Scene");

            Assert.AreEqual(LevelFlowState.Unloaded, _flow.State);
        }

        [Test]
        public void ReentrantCommand_FromWithinEventHandler_IsBlocked()
        {
            LoadAndReachReady();
            _flow.StartLevel();

            TransitionResult? reentrantResult = null;
            _events.Subscribe<LevelCompletedEvent>(e => reentrantResult = _flow.FailLevel());

            _flow.CompleteLevel();

            Assert.IsTrue(reentrantResult.HasValue);
            Assert.AreEqual(TransitionResult.TransitionBlocked, reentrantResult.Value);
            Assert.AreEqual(LevelFlowState.Completed, _flow.State, "The original command must still finish normally.");
        }

        [Test]
        public void AttemptNumber_IncrementsAcrossRetries_ForTheSameLevel()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            Assert.AreEqual(1, _flow.CurrentSession.AttemptNumber);

            _flow.FailLevel();
            _flow.Retry(reloadScene: false);
            Assert.AreEqual(2, _flow.CurrentSession.AttemptNumber);

            _flow.FailLevel();
            _flow.Retry(reloadScene: false);
            Assert.AreEqual(3, _flow.CurrentSession.AttemptNumber);
        }

        [Test]
        public void PersistedCheckpoint_SurvivesANewServiceInstance_ForTheSameLevel()
        {
            _flow.PersistCheckpoints = true;
            LoadAndReachReady();
            _flow.StartLevel();
            var transform = new Gameplay.Objectives.CheckpointData { Id = "CP1", Position = new Vector3(4, 5, 6), Rotation = Quaternion.identity };
            _flow.RegisterCheckpoint("CP1", transform);
            _flow.ActivateCheckpoint("CP1");

            // Simulate an application restart: a brand-new service sharing the same persistence
            // backend/level definition.
            var restarted = new GameFlowService();
            restarted.Initialize(_registry);
            restarted.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");

            bool found = restarted.TryLoadPersistedCheckpoint(out string checkpointId, out Gameplay.Objectives.CheckpointData? loadedTransform);

            Assert.IsTrue(found);
            Assert.AreEqual("CP1", checkpointId);
            Assert.IsTrue(loadedTransform.HasValue);
            Assert.AreEqual(new Vector3(4, 5, 6), loadedTransform.Value.Position);

            restarted.Shutdown();
        }

        [Test]
        public void TryLoadPersistedCheckpoint_WithoutOptingIn_ReturnsFalse()
        {
            LoadAndReachReady();
            _flow.StartLevel();
            _flow.RegisterCheckpoint("CP1");
            _flow.ActivateCheckpoint("CP1"); // PersistCheckpoints left false

            bool found = _flow.TryLoadPersistedCheckpoint(out _, out _);

            Assert.IsFalse(found);
        }
    }
}
