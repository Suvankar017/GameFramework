using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using NUnit.Framework;

namespace GameFramework.Gameplay.Tests
{
    public class GameplayServiceTests
    {
        private sealed class RecordingParticipant : IGameplayLifecycle
        {
            public int InitializeCount;
            public int BeginPlayCount;
            public int PauseCount;
            public int ResumeCount;
            public int ShutdownCount;

            public void OnGameplayInitialize() => InitializeCount++;
            public void OnGameplayBeginPlay() => BeginPlayCount++;
            public void OnGameplayPause() => PauseCount++;
            public void OnGameplayResume() => ResumeCount++;
            public void OnGameplayShutdown() => ShutdownCount++;
        }

        private sealed class RecordingTickable : IGameplayTickable
        {
            public int TickCount;
            public float LastDeltaTime;

            public void GameplayTick(float deltaTime)
            {
                TickCount++;
                LastDeltaTime = deltaTime;
            }
        }

        private FakeTimeService _time;
        private GameplayService _gameplay;

        [SetUp]
        public void SetUp()
        {
            var registry = new ServiceRegistry();
            _time = new FakeTimeService();
            registry.Register<ITimeService>(_time);
            registry.MarkInitialized(typeof(ITimeService));

            _gameplay = new GameplayService();
            _gameplay.Initialize(registry);
        }

        [TearDown]
        public void TearDown()
        {
            _gameplay.Shutdown();
        }

        [Test]
        public void InitialState_IsInactive()
        {
            Assert.AreEqual(GameplayLoopState.Inactive, _gameplay.State);
            Assert.IsFalse(_gameplay.IsActive);
        }

        [Test]
        public void BeginPlay_TransitionsToPlaying_AndNotifiesParticipants()
        {
            var participant = new RecordingParticipant();
            _gameplay.RegisterLifecycle(participant);

            _gameplay.BeginPlay();

            Assert.AreEqual(GameplayLoopState.Playing, _gameplay.State);
            Assert.IsTrue(_gameplay.IsActive);
            Assert.AreEqual(1, participant.BeginPlayCount);
        }

        [Test]
        public void RegisterLifecycle_AlwaysCallsInitializeOnce()
        {
            var participant = new RecordingParticipant();

            _gameplay.RegisterLifecycle(participant);

            Assert.AreEqual(1, participant.InitializeCount);
        }

        [Test]
        public void RegisterLifecycle_WhileAlreadyPlaying_ImmediatelyCallsBeginPlay()
        {
            _gameplay.BeginPlay();
            var lateJoiner = new RecordingParticipant();

            _gameplay.RegisterLifecycle(lateJoiner);

            Assert.AreEqual(1, lateJoiner.InitializeCount);
            Assert.AreEqual(1, lateJoiner.BeginPlayCount);
        }

        [Test]
        public void EndPlay_NotifiesShutdownAndStopsSession()
        {
            var participant = new RecordingParticipant();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.BeginPlay();

            _gameplay.EndPlay();

            Assert.AreEqual(GameplayLoopState.Inactive, _gameplay.State);
            Assert.AreEqual(1, participant.ShutdownCount);
        }

        [Test]
        public void BeginPlay_CalledTwice_SecondCallIsIgnored()
        {
            var participant = new RecordingParticipant();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.BeginPlay();

            _gameplay.BeginPlay();

            Assert.AreEqual(1, participant.BeginPlayCount);
        }

        [Test]
        public void EndPlay_WithoutActiveSession_IsIgnored()
        {
            Assert.DoesNotThrow(() => _gameplay.EndPlay());
        }

        [Test]
        public void UnregisterLifecycle_CallsShutdownAndStopsFurtherNotifications()
        {
            var participant = new RecordingParticipant();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.BeginPlay();

            _gameplay.UnregisterLifecycle(participant);
            _gameplay.EndPlay();

            Assert.AreEqual(1, participant.ShutdownCount); // from Unregister, not doubled by EndPlay
        }

        [Test]
        public void Tick_WhileInactive_DoesNotTickTickables()
        {
            var tickable = new RecordingTickable();
            _gameplay.RegisterTickable(tickable);

            _gameplay.Tick();

            Assert.AreEqual(0, tickable.TickCount);
        }

        [Test]
        public void Tick_WhilePlaying_TicksWithScaledDeltaTime()
        {
            var tickable = new RecordingTickable();
            _gameplay.RegisterTickable(tickable);
            _gameplay.BeginPlay();
            _time.ScaledDeltaTime = 0.25f;

            _gameplay.Tick();

            Assert.AreEqual(1, tickable.TickCount);
            Assert.AreEqual(0.25f, tickable.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void Tick_DetectsPause_NotifiesAndStopsTicking()
        {
            var participant = new RecordingParticipant();
            var tickable = new RecordingTickable();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.RegisterTickable(tickable);
            _gameplay.BeginPlay();

            _time.Pause();
            _gameplay.Tick();

            Assert.AreEqual(GameplayLoopState.Paused, _gameplay.State);
            Assert.AreEqual(1, participant.PauseCount);
            Assert.AreEqual(0, tickable.TickCount);
        }

        [Test]
        public void Tick_DetectsResume_NotifiesAndResumesTicking()
        {
            var participant = new RecordingParticipant();
            var tickable = new RecordingTickable();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.RegisterTickable(tickable);
            _gameplay.BeginPlay();
            _time.Pause();
            _gameplay.Tick();

            _time.Resume();
            _gameplay.Tick();

            Assert.AreEqual(GameplayLoopState.Playing, _gameplay.State);
            Assert.AreEqual(1, participant.ResumeCount);
            Assert.AreEqual(1, tickable.TickCount);
        }

        [Test]
        public void UnregisterTickable_StopsFurtherTicks()
        {
            var tickable = new RecordingTickable();
            _gameplay.RegisterTickable(tickable);
            _gameplay.BeginPlay();

            _gameplay.UnregisterTickable(tickable);
            _gameplay.Tick();

            Assert.AreEqual(0, tickable.TickCount);
        }

        [Test]
        public void Shutdown_WhileSessionActive_EndsSessionFirst()
        {
            var participant = new RecordingParticipant();
            _gameplay.RegisterLifecycle(participant);
            _gameplay.BeginPlay();

            _gameplay.Shutdown();

            Assert.AreEqual(1, participant.ShutdownCount);
        }
    }
}
