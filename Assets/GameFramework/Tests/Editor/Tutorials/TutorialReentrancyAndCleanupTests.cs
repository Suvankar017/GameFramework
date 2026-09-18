using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>Covers re-entrancy protection (CLAUDE.md's Phase 9 brief, section 33) and
    /// deterministic cleanup of subscriptions/pause/input-gating (section 32).</summary>
    public class TutorialReentrancyAndCleanupTests
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

        private void RegisterTutorial(string id, int stepCount, TutorialRepeatPolicy repeatPolicy = TutorialRepeatPolicy.Repeatable)
        {
            TutorialDefinition definition = TestDefinitions.Tutorial(id, repeatPolicy: repeatPolicy);
            _createdAssets.Add(definition);

            var steps = new List<TutorialStepEntry>();
            for (int i = 0; i < stepCount; i++)
            {
                TutorialStepDefinition stepDefinition = TestDefinitions.Step($"{id}.Step{i}");
                _createdAssets.Add(stepDefinition);
                steps.Add(new TutorialStepEntry(stepDefinition, new InstructionStep($"{id}.Step{i}")));
            }

            _tutorials.RegisterTutorial(definition, steps);
        }

        [Test]
        public void Restart_CalledFromInsideStepCompletedHandler_IsBlocked_AndDoesNotCorruptState()
        {
            RegisterTutorial("A", 2);
            _tutorials.Start(new TutorialId("A"));

            TutorialCommandResult nestedResult = TutorialCommandResult.Success;
            _events.Subscribe<TutorialStepCompletedEvent>(e => nestedResult = _tutorials.Restart(new TutorialId("A")));

            _tutorials.CompleteCurrentStep();

            Assert.AreEqual(TutorialCommandResult.Blocked, nestedResult);
            // The original, non-reentrant advance must still have gone through normally.
            Assert.AreEqual("A.Step1", _tutorials.ActiveStepId);
        }

        [Test]
        public void Start_CalledFromInsideCompletedEventHandler_IsBlocked()
        {
            RegisterTutorial("A", 1);
            RegisterTutorial("B", 1);
            TutorialStartResult nestedResult = TutorialStartResult.Success;
            _events.Subscribe<TutorialCompletedEvent>(e => nestedResult = _tutorials.Start(new TutorialId("B")));

            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep(); // finishes A's only step -> tutorial completes -> event fires

            Assert.AreEqual(TutorialStartResult.Blocked, nestedResult);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
        }

        [Test]
        public void Cancel_CalledFromInsideStartedEventHandler_IsBlocked()
        {
            RegisterTutorial("A", 1);
            TutorialCommandResult nestedResult = TutorialCommandResult.Success;
            _events.Subscribe<TutorialStartedEvent>(e => nestedResult = _tutorials.Cancel());

            _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialCommandResult.Blocked, nestedResult);
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
        }

        [Test]
        public void MultipleZeroDurationSteps_CascadeInOrder_WithinOneCommand()
        {
            TutorialDefinition definition = TestDefinitions.Tutorial("A");
            _createdAssets.Add(definition);
            var steps = new List<TutorialStepEntry>();
            var order = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                TutorialStepDefinition stepDefinition = TestDefinitions.Step($"A.Step{i}");
                _createdAssets.Add(stepDefinition);
                steps.Add(new TutorialStepEntry(stepDefinition, new WaitStep($"A.Step{i}", 0f)));
            }
            _tutorials.RegisterTutorial(definition, steps);
            _events.Subscribe<TutorialStepStartedEvent>(e => order.Add($"Started:{e.StepId}"));
            _events.Subscribe<TutorialStepCompletedEvent>(e => order.Add($"Completed:{e.StepId}"));

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.Success, result);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
            Assert.AreEqual(new[]
            {
                "Started:A.Step0", "Completed:A.Step0",
                "Started:A.Step1", "Completed:A.Step1",
                "Started:A.Step2", "Completed:A.Step2"
            }, order.ToArray());
        }

        [Test]
        public void EventStep_CancelledMidTutorial_UnsubscribesSoALaterPublishHasNoEffect()
        {
            TutorialDefinition definition = TestDefinitions.Tutorial("A");
            TutorialStepDefinition stepDefinition = TestDefinitions.Step("A.Wait");
            _createdAssets.Add(definition);
            _createdAssets.Add(stepDefinition);
            var step = new EventStep<TestSignal>("A.Wait", _events);
            _tutorials.RegisterTutorial(definition, new List<TutorialStepEntry> { new TutorialStepEntry(stepDefinition, step) });
            _tutorials.Start(new TutorialId("A"));

            _tutorials.Cancel();

            Assert.DoesNotThrow(() => _events.Publish(new TestSignal()));
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
        }

        [Test]
        public void Cancel_CalledTwice_SecondCallReturnsNoActiveTutorial_AndDoesNotDoubleReleasePause()
        {
            TutorialDefinition definition = TestDefinitions.Tutorial("A", pausePolicy: TutorialPausePolicy.PausesGameplay);
            TutorialStepDefinition stepDefinition = TestDefinitions.Step("A.Step0");
            _createdAssets.Add(definition);
            _createdAssets.Add(stepDefinition);
            _tutorials.RegisterTutorial(definition, new List<TutorialStepEntry>
            {
                new TutorialStepEntry(stepDefinition, new InstructionStep("A.Step0"))
            });
            _tutorials.Start(new TutorialId("A"));

            _tutorials.Cancel();
            TutorialCommandResult second = _tutorials.Cancel();

            Assert.AreEqual(TutorialCommandResult.NoActiveTutorial, second);
            Assert.IsFalse(_time.IsPaused);
        }

        [Test]
        public void Skip_WhilePaused_Succeeds_AndReturnsToInactive()
        {
            RegisterTutorial("A", 1);
            _tutorials.Start(new TutorialId("A"));
            _time.Pause();
            _tutorials.Tick();
            Assert.AreEqual(TutorialState.Paused, _tutorials.State);

            TutorialCommandResult result = _tutorials.Skip();

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
        }

        private readonly struct TestSignal
        {
        }
    }
}
