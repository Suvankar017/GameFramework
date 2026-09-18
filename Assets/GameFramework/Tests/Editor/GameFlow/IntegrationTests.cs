using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.GameFlow.Tests
{
    /// <summary>Full-flow scenarios named after CLAUDE.md's Phase 8 brief (section 71) - each test
    /// walks one complete, realistic sequence end to end rather than isolating one command.</summary>
    public class IntegrationTests
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

        [Test]
        public void Flow1_LoadReadyStartPlayingCompleteResults()
        {
            Assert.AreEqual(LevelLoadResult.Started, _flow.LoadLevel(_level));
            Assert.AreEqual(LevelFlowState.Loading, _flow.State);

            _scene.RaiseSceneLoaded("Level1Scene");
            Assert.AreEqual(LevelFlowState.Ready, _flow.State);

            Assert.AreEqual(TransitionResult.Success, _flow.StartLevel());
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);

            Assert.AreEqual(TransitionResult.Success, _flow.CompleteLevel(GameplayResult.Success("Finished")));
            Assert.AreEqual(LevelFlowState.Completed, _flow.State);
            Assert.AreEqual(GameplayResultKind.Success, _flow.CurrentSession.Result.Kind);
        }

        [Test]
        public void Flow2_PlayingPauseResumePlaying()
        {
            _flow.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");
            _flow.StartLevel();

            _time.Pause();
            _flow.Tick();
            Assert.AreEqual(LevelFlowState.Paused, _flow.State);

            _time.Resume();
            _flow.Tick();
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
        }

        [Test]
        public void Flow3_PlayingCheckpointFailureRespawnPlaying()
        {
            _flow.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");
            _flow.StartLevel();

            var checkpointTransform = new CheckpointData { Id = "CP1", Position = new Vector3(10, 0, 0), Rotation = Quaternion.identity };
            _flow.RegisterCheckpoint("CP1", checkpointTransform);
            Assert.IsTrue(_flow.ActivateCheckpoint("CP1"));

            Assert.AreEqual(TransitionResult.Success, _flow.FailLevel(GameplayResult.Failure("PlayerDied")));
            Assert.AreEqual(LevelFlowState.Failed, _flow.State);

            Assert.AreEqual(RespawnResult.Success, _flow.Respawn());
            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreEqual(1, _flow.CurrentSession.AttemptNumber, "Respawn must not start a new attempt.");
        }

        [Test]
        public void Flow4_PlayingFailureRetryNewSessionPlaying()
        {
            _flow.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");
            _flow.StartLevel();
            Session.SessionId firstSessionId = _flow.CurrentSession.Id;

            _flow.FailLevel();
            Assert.AreEqual(TransitionResult.Success, _flow.Retry(reloadScene: false));

            Assert.AreEqual(LevelFlowState.Playing, _flow.State);
            Assert.AreNotEqual(firstSessionId, _flow.CurrentSession.Id, "Retry must create a new session.");
            Assert.AreEqual(2, _flow.CurrentSession.AttemptNumber);
        }

        [Test]
        public void Flow5_CompleteSaveLoadCheckpointStillResolvesAfterRestart()
        {
            _flow.PersistCheckpoints = true;
            _flow.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");
            _flow.StartLevel();

            _flow.RegisterCheckpoint("CP1", new CheckpointData { Id = "CP1", Position = Vector3.one, Rotation = Quaternion.identity });
            _flow.ActivateCheckpoint("CP1");
            _flow.CompleteLevel();

            // "Save -> load -> progression remains valid" for GameFlow's own bounded persistence:
            // a fresh service instance backed by the same PersistenceService can still resolve it.
            var reloaded = new GameFlowService();
            reloaded.Initialize(_registry);
            reloaded.LoadLevel(_level);
            _scene.RaiseSceneLoaded("Level1Scene");

            bool found = reloaded.TryLoadPersistedCheckpoint(out string checkpointId, out CheckpointData? transform);

            Assert.IsTrue(found);
            Assert.AreEqual("CP1", checkpointId);
            Assert.AreEqual(Vector3.one, transform.Value.Position);

            reloaded.Shutdown();
        }
    }
}
