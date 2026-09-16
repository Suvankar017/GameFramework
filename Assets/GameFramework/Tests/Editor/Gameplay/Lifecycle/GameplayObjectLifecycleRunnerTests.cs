using GameFramework.Gameplay.Lifecycle;
using NUnit.Framework;

namespace GameFramework.Gameplay.Tests
{
    public class GameplayObjectLifecycleRunnerTests
    {
        private sealed class RecordingLifecycle : IGameplayObjectLifecycle
        {
            public int InitializeCount;
            public int ActivateCount;
            public int DeactivateCount;
            public int DisposeCount;

            public void Initialize() => InitializeCount++;
            public void Activate() => ActivateCount++;
            public void Deactivate() => DeactivateCount++;
            public void Dispose() => DisposeCount++;
        }

        [Test]
        public void InitialState_IsCreated()
        {
            var runner = new GameplayObjectLifecycleRunner(new RecordingLifecycle());

            Assert.AreEqual(GameplayObjectLifecycleState.Created, runner.State);
        }

        [Test]
        public void Initialize_CallsTargetOnce()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);

            runner.Initialize();

            Assert.AreEqual(1, target.InitializeCount);
            Assert.AreEqual(GameplayObjectLifecycleState.Initialized, runner.State);
        }

        [Test]
        public void Initialize_CalledTwice_SecondCallIsIgnored()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);

            runner.Initialize();
            runner.Initialize();

            Assert.AreEqual(1, target.InitializeCount);
        }

        [Test]
        public void Activate_BeforeInitialize_IsIgnored()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);

            runner.Activate();

            Assert.AreEqual(0, target.ActivateCount);
            Assert.AreEqual(GameplayObjectLifecycleState.Created, runner.State);
        }

        [Test]
        public void FullCycle_InitializeActivateDeactivateReuse_WorksLikeAPooledObject()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);

            runner.Initialize();
            runner.Activate();
            runner.Deactivate();
            runner.Activate(); // simulates a second pool Get
            runner.Deactivate(); // simulates a pool Release

            Assert.AreEqual(1, target.InitializeCount, "Initialize must fire only once across reuse.");
            Assert.AreEqual(2, target.ActivateCount);
            Assert.AreEqual(2, target.DeactivateCount);
            Assert.AreEqual(GameplayObjectLifecycleState.Deactivated, runner.State);
        }

        [Test]
        public void Deactivate_WhenNotActive_IsSilentlyIgnored()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);

            Assert.DoesNotThrow(() => runner.Deactivate());
            Assert.AreEqual(0, target.DeactivateCount);
        }

        [Test]
        public void Dispose_FromActive_DeactivatesFirstThenDisposes()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);
            runner.Initialize();
            runner.Activate();

            runner.Dispose();

            Assert.AreEqual(1, target.DeactivateCount);
            Assert.AreEqual(1, target.DisposeCount);
            Assert.AreEqual(GameplayObjectLifecycleState.Destroyed, runner.State);
        }

        [Test]
        public void Dispose_CalledTwice_SecondCallIsIgnored()
        {
            var target = new RecordingLifecycle();
            var runner = new GameplayObjectLifecycleRunner(target);
            runner.Initialize();

            runner.Dispose();
            runner.Dispose();

            Assert.AreEqual(1, target.DisposeCount);
        }
    }
}
