using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    public class TutorialPauseAndInputGatingTests
    {
        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private FakeInputService _input;
        private EventService _events;
        private PersistenceService _persistence;
        private TutorialService _tutorials;
        private readonly List<Object> _createdAssets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _time, out _input, out _events, out _persistence);
            _tutorials = new TutorialService();
            _tutorials.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _tutorials.Shutdown();
            foreach (Object asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        private TutorialDefinition RegisterSingleWaitStepTutorial(
            string id, float durationSeconds, TutorialPausePolicy pausePolicy, InputContextDefinition? inputContext = null)
        {
            TutorialDefinition definition = TestDefinitions.Tutorial(id, pausePolicy: pausePolicy);
            TutorialStepDefinition stepDefinition = TestDefinitions.Step($"{id}.Wait");
            _createdAssets.Add(definition);
            _createdAssets.Add(stepDefinition);

            var steps = new List<TutorialStepEntry> { new TutorialStepEntry(stepDefinition, new WaitStep($"{id}.Wait", durationSeconds)) };
            _tutorials.RegisterTutorial(definition, steps, inputContext);
            return definition;
        }

        [Test]
        public void PausesGameplay_Start_AcquiresPause_WithoutFlippingOwnStateToPaused()
        {
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.PausesGameplay);

            _tutorials.Start(new TutorialId("A"));

            Assert.IsTrue(_time.IsPaused);
            Assert.AreEqual(TutorialState.Running, _tutorials.State, "A tutorial must not observe its own PausesGameplay acquisition as an external pause.");
        }

        [Test]
        public void PausesGameplay_Completion_ReleasesPauseExactlyOnce()
        {
            RegisterSingleWaitStepTutorial("A", 0f, TutorialPausePolicy.PausesGameplay);

            _tutorials.Start(new TutorialId("A")); // zero-duration WaitStep completes on Begin -> tutorial finishes immediately

            Assert.IsFalse(_time.IsPaused);
        }

        [Test]
        public void PausesGameplay_Cancel_ReleasesPause()
        {
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.PausesGameplay);
            _tutorials.Start(new TutorialId("A"));

            _tutorials.Cancel();

            Assert.IsFalse(_time.IsPaused);
        }

        [Test]
        public void DoesNotPauseGameplay_ExternalPause_ReactivelyEntersPausedState()
        {
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.DoesNotPauseGameplay);
            _tutorials.Start(new TutorialId("A"));
            TutorialId pausedId = default;
            _events.Subscribe<TutorialPausedEvent>(e => pausedId = e.TutorialId);

            _time.Pause(); // an unrelated system (e.g. a pause menu) pauses gameplay
            _tutorials.Tick();

            Assert.AreEqual(TutorialState.Paused, _tutorials.State);
            Assert.AreEqual(new TutorialId("A"), pausedId);
        }

        [Test]
        public void DoesNotPauseGameplay_ExternalResume_ReactivelyReturnsToRunning()
        {
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.DoesNotPauseGameplay);
            _tutorials.Start(new TutorialId("A"));
            _time.Pause();
            _tutorials.Tick();
            TutorialId resumedId = default;
            _events.Subscribe<TutorialResumedEvent>(e => resumedId = e.TutorialId);

            _time.Resume();
            _tutorials.Tick();

            Assert.AreEqual(TutorialState.Running, _tutorials.State);
            Assert.AreEqual(new TutorialId("A"), resumedId);
        }

        [Test]
        public void WhilePaused_ActiveStepTick_DoesNotAdvanceTime()
        {
            RegisterSingleWaitStepTutorial("A", 3f, TutorialPausePolicy.DoesNotPauseGameplay);
            _tutorials.Start(new TutorialId("A"));
            _time.Pause();
            _tutorials.Tick(); // observes pause, transitions to Paused

            _time.UnscaledDeltaTime = 10f;
            _tutorials.Tick(); // must not tick the WaitStep while Paused

            Assert.AreEqual(TutorialState.Paused, _tutorials.State);
            Assert.AreEqual("A.Wait", _tutorials.ActiveStepId);

            _time.Resume();
            _tutorials.Tick(); // back to Running
            _tutorials.Tick(); // now the WaitStep actually advances

            Assert.AreEqual(TutorialState.Inactive, _tutorials.State, "The WaitStep should now have completed and finished the tutorial.");
        }

        [Test]
        public void InputContext_PushedOnStart_AndPoppedOnCompletion()
        {
            InputContextDefinition context = InputContextDefinition.Restricted("TutorialA", "Confirm");
            RegisterSingleWaitStepTutorial("A", 0f, TutorialPausePolicy.DoesNotPauseGameplay, context);

            _tutorials.Start(new TutorialId("A")); // zero-duration step finishes immediately

            Assert.AreEqual(1, _input.PushContextCallCount);
            Assert.AreEqual(1, _input.PopContextCallCount);
            Assert.AreEqual("Gameplay", _input.CurrentContextName);
        }

        [Test]
        public void InputContext_PoppedOnCancel()
        {
            InputContextDefinition context = InputContextDefinition.Restricted("TutorialA", "Confirm");
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.DoesNotPauseGameplay, context);
            _tutorials.Start(new TutorialId("A"));
            Assert.AreEqual("TutorialA", _input.CurrentContextName);

            _tutorials.Cancel();

            Assert.AreEqual("Gameplay", _input.CurrentContextName);
        }

        [Test]
        public void Shutdown_MidTutorial_ReleasesPauseAndInputContext()
        {
            InputContextDefinition context = InputContextDefinition.Restricted("TutorialA", "Confirm");
            RegisterSingleWaitStepTutorial("A", 5f, TutorialPausePolicy.PausesGameplay, context);
            _tutorials.Start(new TutorialId("A"));

            _tutorials.Shutdown();

            Assert.IsFalse(_time.IsPaused);
            Assert.AreEqual("Gameplay", _input.CurrentContextName);
        }
    }
}
