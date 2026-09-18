using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    public class TutorialServiceTests
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

        private TutorialDefinition Track(TutorialDefinition definition)
        {
            _createdAssets.Add(definition);
            return definition;
        }

        private TutorialStepDefinition Track(TutorialStepDefinition definition)
        {
            _createdAssets.Add(definition);
            return definition;
        }

        /// <summary>Registers a two-step, manually-completed tutorial ("A") - both steps only
        /// advance via <see cref="ITutorialService.CompleteCurrentStep"/>, so tests can drive
        /// sequencing deterministically without any input/event/condition/timer plumbing.</summary>
        private TutorialDefinition RegisterTwoStepTutorial(
            string id = "A",
            TutorialRepeatPolicy repeatPolicy = TutorialRepeatPolicy.Once,
            TutorialSkipPolicy skipPolicy = TutorialSkipPolicy.Skippable,
            TutorialPausePolicy pausePolicy = TutorialPausePolicy.DoesNotPauseGameplay,
            TutorialPersistencePolicy persistencePolicy = TutorialPersistencePolicy.CompletionOnly,
            string[] prerequisites = null)
        {
            TutorialDefinition definition = Track(TestDefinitions.Tutorial(id, prerequisites, repeatPolicy, skipPolicy, pausePolicy, persistencePolicy));
            TutorialStepDefinition step1 = Track(TestDefinitions.Step($"{id}.Step1"));
            TutorialStepDefinition step2 = Track(TestDefinitions.Step($"{id}.Step2"));

            var steps = new List<TutorialStepEntry>
            {
                new TutorialStepEntry(step1, new InstructionStep($"{id}.Step1")),
                new TutorialStepEntry(step2, new InstructionStep($"{id}.Step2"))
            };

            _tutorials.RegisterTutorial(definition, steps);
            return definition;
        }

        [Test]
        public void InitialState_IsInactive()
        {
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
            Assert.IsFalse(_tutorials.ActiveTutorialId.IsValid);
            Assert.AreEqual(-1, _tutorials.ActiveStepIndex);
            Assert.AreEqual(0, _tutorials.ActiveStepCount);
        }

        [Test]
        public void Start_UnregisteredTutorial_ReturnsNotFound()
        {
            TutorialStartResult result = _tutorials.Start(new TutorialId("Nope"));

            Assert.AreEqual(TutorialStartResult.NotFound, result);
        }

        [Test]
        public void Start_Registered_TransitionsToRunning_AndBeginsFirstStep()
        {
            RegisterTwoStepTutorial();

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.Success, result);
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
            Assert.AreEqual(new TutorialId("A"), _tutorials.ActiveTutorialId);
            Assert.AreEqual("A.Step1", _tutorials.ActiveStepId);
            Assert.AreEqual(0, _tutorials.ActiveStepIndex);
            Assert.AreEqual(2, _tutorials.ActiveStepCount);
        }

        [Test]
        public void Start_WhileSameTutorialActive_ReturnsAlreadyRunning()
        {
            RegisterTwoStepTutorial();
            _tutorials.Start(new TutorialId("A"));

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.AlreadyRunning, result);
        }

        [Test]
        public void Start_WhileDifferentTutorialActive_ReturnsBlocked()
        {
            RegisterTwoStepTutorial("A");
            RegisterTwoStepTutorial("B");
            _tutorials.Start(new TutorialId("A"));

            TutorialStartResult result = _tutorials.Start(new TutorialId("B"));

            Assert.AreEqual(TutorialStartResult.Blocked, result);
        }

        [Test]
        public void CompleteCurrentStep_AdvancesToNextStep_AndPublishesEvents()
        {
            RegisterTwoStepTutorial();
            var started = new List<string>();
            var completed = new List<string>();
            _events.Subscribe<TutorialStepStartedEvent>(e => started.Add(e.StepId));
            _events.Subscribe<TutorialStepCompletedEvent>(e => completed.Add(e.StepId));

            _tutorials.Start(new TutorialId("A"));
            started.Clear(); // drop the first step's own Started event for a clean assertion below

            TutorialCommandResult result = _tutorials.CompleteCurrentStep();

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.AreEqual(new[] { "A.Step1" }, completed.ToArray());
            Assert.AreEqual(new[] { "A.Step2" }, started.ToArray());
            Assert.AreEqual(1, _tutorials.ActiveStepIndex);
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
        }

        [Test]
        public void CompletingLastStep_CompletesTutorial_AndPublishesTutorialCompletedEvent()
        {
            RegisterTwoStepTutorial();
            TutorialId completedId = default;
            _events.Subscribe<TutorialCompletedEvent>(e => completedId = e.TutorialId);
            _tutorials.Start(new TutorialId("A"));

            _tutorials.CompleteCurrentStep();
            TutorialCommandResult result = _tutorials.CompleteCurrentStep();

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.AreEqual(new TutorialId("A"), completedId);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
            Assert.IsTrue(_tutorials.HasCompleted(new TutorialId("A")));
            Assert.IsFalse(_tutorials.WasSkipped(new TutorialId("A")));
        }

        [Test]
        public void CompleteCurrentStep_WithNoActiveTutorial_ReturnsNoActiveTutorial()
        {
            TutorialCommandResult result = _tutorials.CompleteCurrentStep();

            Assert.AreEqual(TutorialCommandResult.NoActiveTutorial, result);
        }

        [Test]
        public void Skip_MarksCompletedAndSkipped_PublishesSkippedNotCompleted()
        {
            RegisterTwoStepTutorial();
            bool completedFired = false;
            TutorialId skippedId = default;
            _events.Subscribe<TutorialCompletedEvent>(e => completedFired = true);
            _events.Subscribe<TutorialSkippedEvent>(e => skippedId = e.TutorialId);
            _tutorials.Start(new TutorialId("A"));

            TutorialCommandResult result = _tutorials.Skip();

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.IsFalse(completedFired);
            Assert.AreEqual(new TutorialId("A"), skippedId);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
            Assert.IsTrue(_tutorials.HasCompleted(new TutorialId("A")));
            Assert.IsTrue(_tutorials.WasSkipped(new TutorialId("A")));
        }

        [Test]
        public void Skip_NotSkippableTutorial_ReturnsNotSkippable_AndStaysRunning()
        {
            RegisterTwoStepTutorial(skipPolicy: TutorialSkipPolicy.NotSkippable);
            _tutorials.Start(new TutorialId("A"));

            TutorialCommandResult result = _tutorials.Skip();

            Assert.AreEqual(TutorialCommandResult.NotSkippable, result);
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
        }

        [Test]
        public void Skip_WithNoActiveTutorial_ReturnsNoActiveTutorial()
        {
            TutorialCommandResult result = _tutorials.Skip();

            Assert.AreEqual(TutorialCommandResult.NoActiveTutorial, result);
        }

        [Test]
        public void Cancel_DoesNotMarkCompleted_AndReleasesSlotForAnotherTutorial()
        {
            RegisterTwoStepTutorial("A");
            RegisterTwoStepTutorial("B");
            TutorialId cancelledId = default;
            _events.Subscribe<TutorialCancelledEvent>(e => cancelledId = e.TutorialId);
            _tutorials.Start(new TutorialId("A"));

            TutorialCommandResult cancelResult = _tutorials.Cancel();

            Assert.AreEqual(TutorialCommandResult.Success, cancelResult);
            Assert.AreEqual(new TutorialId("A"), cancelledId);
            Assert.IsFalse(_tutorials.HasCompleted(new TutorialId("A")));
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);

            TutorialStartResult startB = _tutorials.Start(new TutorialId("B"));
            Assert.AreEqual(TutorialStartResult.Success, startB);
        }

        [Test]
        public void Cancel_ThenStartAgain_BeginsFromFirstStep()
        {
            RegisterTwoStepTutorial();
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep(); // now on Step2
            _tutorials.Cancel();

            _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual("A.Step1", _tutorials.ActiveStepId);
        }

        [Test]
        public void Cancel_WithNoActiveTutorial_ReturnsNoActiveTutorial()
        {
            TutorialCommandResult result = _tutorials.Cancel();

            Assert.AreEqual(TutorialCommandResult.NoActiveTutorial, result);
        }

        [Test]
        public void Restart_UnregisteredTutorial_ReturnsNotFound()
        {
            TutorialCommandResult result = _tutorials.Restart(new TutorialId("Nope"));

            Assert.AreEqual(TutorialCommandResult.NotFound, result);
        }

        [Test]
        public void Restart_WhileMidRun_BypassesRepeatPolicyAndBeginsAtFirstStep()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Once);
            var restarted = new List<TutorialId>();
            _events.Subscribe<TutorialRestartedEvent>(e => restarted.Add(e.TutorialId));
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep(); // now on Step2

            TutorialCommandResult result = _tutorials.Restart(new TutorialId("A"));

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.AreEqual(new[] { new TutorialId("A") }, restarted.ToArray());
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
            Assert.AreEqual("A.Step1", _tutorials.ActiveStepId);
        }

        [Test]
        public void Restart_AfterOnceCompletion_BypassesRepeatPolicy()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Once);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep(); // fully completed, Once policy now blocks Start()
            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, _tutorials.Start(new TutorialId("A")));

            TutorialCommandResult result = _tutorials.Restart(new TutorialId("A"));

            Assert.AreEqual(TutorialCommandResult.Success, result);
            Assert.AreEqual(TutorialState.Running, _tutorials.State);
            Assert.IsFalse(_tutorials.HasCompleted(new TutorialId("A")));
        }

        [Test]
        public void Restart_WhileDifferentTutorialActive_ReturnsBlocked()
        {
            RegisterTwoStepTutorial("A");
            RegisterTwoStepTutorial("B");
            _tutorials.Start(new TutorialId("A"));

            TutorialCommandResult result = _tutorials.Restart(new TutorialId("B"));

            Assert.AreEqual(TutorialCommandResult.Blocked, result);
        }

        [Test]
        public void RepeatPolicy_Once_BlocksSecondStart_AfterCompletion()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Once);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, result);
        }

        [Test]
        public void RepeatPolicy_Repeatable_AllowsSecondStart_AfterCompletion()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Repeatable);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.Success, result);
        }

        [Test]
        public void RepeatPolicy_Repeatable_SecondFullRun_ProgressesThroughBothStepsAgain()
        {
            // Regression coverage: the same ITutorialStep *instances* are reused across every run
            // of a tutorial, so a naive implementation can silently skip/short-circuit a second run
            // because each step already reached a terminal state the first time.
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Repeatable);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);

            _tutorials.Start(new TutorialId("A"));
            Assert.AreEqual("A.Step1", _tutorials.ActiveStepId);

            TutorialCommandResult afterFirstStep = _tutorials.CompleteCurrentStep();
            Assert.AreEqual(TutorialCommandResult.Success, afterFirstStep);
            Assert.AreEqual("A.Step2", _tutorials.ActiveStepId);

            TutorialCommandResult afterSecondStep = _tutorials.CompleteCurrentStep();
            Assert.AreEqual(TutorialCommandResult.Success, afterSecondStep);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
        }

        [Test]
        public void RepeatPolicy_OncePerSession_BlocksASecondStart_WithinSameSession()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.OncePerSession);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();

            TutorialStartResult result = _tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, result);
        }

        [Test]
        public void Prerequisite_NotCompleted_BlocksStart()
        {
            RegisterTwoStepTutorial("A");
            RegisterTwoStepTutorial("B", prerequisites: new[] { "A" });

            TutorialStartResult result = _tutorials.Start(new TutorialId("B"));

            Assert.AreEqual(TutorialStartResult.PrerequisiteNotMet, result);
        }

        [Test]
        public void Prerequisite_Completed_AllowsStart()
        {
            RegisterTwoStepTutorial("A");
            RegisterTwoStepTutorial("B", prerequisites: new[] { "A" });
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();

            TutorialStartResult result = _tutorials.Start(new TutorialId("B"));

            Assert.AreEqual(TutorialStartResult.Success, result);
        }

        [Test]
        public void Prerequisite_Unregistered_IsTreatedAsUnsatisfied()
        {
            RegisterTwoStepTutorial("B", prerequisites: new[] { "GhostTutorial" });

            Assert.IsFalse(_tutorials.ArePrerequisitesSatisfied(new TutorialId("B")));
        }

        [Test]
        public void EmptyStepList_CompletesImmediately()
        {
            TutorialDefinition definition = Track(TestDefinitions.Tutorial("Empty"));
            _tutorials.RegisterTutorial(definition, new List<TutorialStepEntry>());

            TutorialStartResult result = _tutorials.Start(new TutorialId("Empty"));

            Assert.AreEqual(TutorialStartResult.Success, result);
            Assert.AreEqual(TutorialState.Inactive, _tutorials.State);
            Assert.IsTrue(_tutorials.HasCompleted(new TutorialId("Empty")));
        }

        [Test]
        public void RegisterTutorial_DuplicateId_Throws()
        {
            RegisterTwoStepTutorial("A");

            Assert.Throws<System.InvalidOperationException>(() => RegisterTwoStepTutorial("A"));
        }

        [Test]
        public void RegisterTutorial_NoId_Throws()
        {
            TutorialDefinition definition = Track(TestDefinitions.Tutorial(null));

            Assert.Throws<System.ArgumentException>(() =>
                _tutorials.RegisterTutorial(definition, new List<TutorialStepEntry>()));
        }

        [Test]
        public void GetDefinition_ReturnsRegisteredAsset_OrNull()
        {
            TutorialDefinition definition = RegisterTwoStepTutorial("A");

            Assert.AreSame(definition, _tutorials.GetDefinition(new TutorialId("A")));
            Assert.IsNull(_tutorials.GetDefinition(new TutorialId("Nope")));
        }

        [Test]
        public void ResetToDefaults_CancelsActiveRun_AndClearsCompletionState()
        {
            RegisterTwoStepTutorial(repeatPolicy: TutorialRepeatPolicy.Once);
            _tutorials.Start(new TutorialId("A"));
            _tutorials.CompleteCurrentStep();
            _tutorials.CompleteCurrentStep();
            Assert.IsTrue(_tutorials.HasCompleted(new TutorialId("A")));

            _tutorials.ResetToDefaults();

            Assert.IsFalse(_tutorials.HasCompleted(new TutorialId("A")));
            Assert.AreEqual(TutorialStartResult.Success, _tutorials.Start(new TutorialId("A")));
        }
    }
}
