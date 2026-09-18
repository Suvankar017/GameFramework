using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials.Conditions;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>End-to-end flows spanning multiple step types and, where noted, multiple
    /// <see cref="TutorialService"/> instances sharing one persisted store (simulating an
    /// application restart) - the composition CLAUDE.md's Phase 9 brief asks Phase 9 to
    /// demonstrate working together, not just in isolation.</summary>
    public class IntegrationTests
    {
        private readonly List<Object> _createdAssets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        private TutorialStepDefinition TrackedStep(string id)
        {
            TutorialStepDefinition step = TestDefinitions.Step(id);
            _createdAssets.Add(step);
            return step;
        }

        private TutorialDefinition TrackedTutorial(
            string id,
            TutorialRepeatPolicy repeatPolicy = TutorialRepeatPolicy.Once,
            TutorialPersistencePolicy persistencePolicy = TutorialPersistencePolicy.CompletionOnly,
            string[] prerequisites = null)
        {
            TutorialDefinition definition = TestDefinitions.Tutorial(id, prerequisites, repeatPolicy, persistencePolicy: persistencePolicy);
            _createdAssets.Add(definition);
            return definition;
        }

        /// <summary>Instruction -> Input -> Event -> Condition -> Instruction, matching this
        /// framework's own "FirstDrive" example (CLAUDE.md's Phase 9 brief, section 4).</summary>
        [Test]
        public void FullSequence_AcrossAllBuiltInStepTypes_CompletesInOrder()
        {
            var registry = TestRegistryFactory.Build(out FakeTimeService time, out FakeInputService input, out EventService events, out PersistenceService persistence);
            var tutorials = new TutorialService();
            tutorials.Initialize(registry);

            TutorialDefinition definition = TrackedTutorial("FirstDrive");
            bool carMoving = false;

            var steps = new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("FirstDrive.Intro"), new InstructionStep("FirstDrive.Intro")),
                new TutorialStepEntry(TrackedStep("FirstDrive.PressMove"), new InputStep("FirstDrive.PressMove", input, "Move", InputTriggerType.Pressed)),
                new TutorialStepEntry(TrackedStep("FirstDrive.WaitForMovement"), new EventStep<CarStartedMovingEvent>("FirstDrive.WaitForMovement", events)),
                new TutorialStepEntry(TrackedStep("FirstDrive.ReachCheckpoint"), new ConditionStep("FirstDrive.ReachCheckpoint", new DelegateTutorialCondition(() => carMoving))),
                new TutorialStepEntry(TrackedStep("FirstDrive.Outro"), new InstructionStep("FirstDrive.Outro"))
            };
            tutorials.RegisterTutorial(definition, steps);

            var completedSteps = new List<string>();
            events.Subscribe<TutorialStepCompletedEvent>(e => completedSteps.Add(e.StepId));
            TutorialId completedTutorial = default;
            events.Subscribe<TutorialCompletedEvent>(e => completedTutorial = e.TutorialId);

            Assert.AreEqual(TutorialStartResult.Success, tutorials.Start(new TutorialId("FirstDrive")));
            Assert.AreEqual("FirstDrive.Intro", tutorials.ActiveStepId);

            tutorials.CompleteCurrentStep(); // player acknowledges the instruction
            Assert.AreEqual("FirstDrive.PressMove", tutorials.ActiveStepId);

            input.SetState("Move", new InputActionState(true, true, false, 0f, UnityEngine.Vector2.zero));
            tutorials.Tick();
            Assert.AreEqual("FirstDrive.WaitForMovement", tutorials.ActiveStepId);

            events.Publish(new CarStartedMovingEvent());
            Assert.AreEqual("FirstDrive.ReachCheckpoint", tutorials.ActiveStepId);

            carMoving = true;
            tutorials.Tick();
            Assert.AreEqual("FirstDrive.Outro", tutorials.ActiveStepId);

            tutorials.CompleteCurrentStep();

            Assert.AreEqual(TutorialState.Inactive, tutorials.State);
            Assert.AreEqual(new TutorialId("FirstDrive"), completedTutorial);
            Assert.AreEqual(5, completedSteps.Count);

            tutorials.Shutdown();
        }

        [Test]
        public void SkipMidTutorial_ThenRestart_RunsFromFirstStepAgain()
        {
            var registry = TestRegistryFactory.Build(out FakeTimeService time, out FakeInputService input, out EventService events, out PersistenceService persistence);
            var tutorials = new TutorialService();
            tutorials.Initialize(registry);

            TutorialDefinition definition = TrackedTutorial("A", repeatPolicy: TutorialRepeatPolicy.Once);
            var steps = new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("A.Step0"), new InstructionStep("A.Step0")),
                new TutorialStepEntry(TrackedStep("A.Step1"), new InstructionStep("A.Step1"))
            };
            tutorials.RegisterTutorial(definition, steps);

            tutorials.Start(new TutorialId("A"));
            tutorials.CompleteCurrentStep();
            tutorials.Skip();

            Assert.IsTrue(tutorials.HasCompleted(new TutorialId("A")));
            Assert.IsTrue(tutorials.WasSkipped(new TutorialId("A")));
            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, tutorials.Start(new TutorialId("A")));

            TutorialCommandResult restartResult = tutorials.Restart(new TutorialId("A"));

            Assert.AreEqual(TutorialCommandResult.Success, restartResult);
            Assert.AreEqual("A.Step0", tutorials.ActiveStepId);
            Assert.IsFalse(tutorials.HasCompleted(new TutorialId("A")));

            tutorials.Shutdown();
        }

        [Test]
        public void CancelMidTutorial_PersistedStateUnaffected_ThenNormalReplayStillWorks()
        {
            var registry = TestRegistryFactory.Build(out FakeTimeService time, out FakeInputService input, out EventService events, out PersistenceService persistence);
            var tutorials = new TutorialService();
            tutorials.Initialize(registry);

            TutorialDefinition definition = TrackedTutorial("A", repeatPolicy: TutorialRepeatPolicy.Repeatable, persistencePolicy: TutorialPersistencePolicy.ResumeProgress);
            var steps = new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("A.Step0"), new InstructionStep("A.Step0")),
                new TutorialStepEntry(TrackedStep("A.Step1"), new InstructionStep("A.Step1"))
            };
            tutorials.RegisterTutorial(definition, steps);

            tutorials.Start(new TutorialId("A"));
            tutorials.CompleteCurrentStep(); // on Step1
            tutorials.Cancel();
            tutorials.Save();

            Assert.IsFalse(tutorials.HasCompleted(new TutorialId("A")));

            TutorialStartResult result = tutorials.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.Success, result);
            Assert.AreEqual("A.Step0", tutorials.ActiveStepId, "A cancelled run must not leave a resume point, even under ResumeProgress.");

            tutorials.Shutdown();
        }

        [Test]
        public void CompleteSaveSimulatedRestartLoad_PrerequisiteGatedTutorial_BecomesStartable()
        {
            var registry = TestRegistryFactory.Build(out FakeTimeService time, out FakeInputService input, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);

            TutorialDefinition onboarding = TrackedTutorial("Onboarding");
            first.RegisterTutorial(onboarding, new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("Onboarding.Step0"), new InstructionStep("Onboarding.Step0"))
            });

            TutorialDefinition feature = TrackedTutorial("FeatureX", prerequisites: new[] { "Onboarding" });
            first.RegisterTutorial(feature, new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("FeatureX.Step0"), new InstructionStep("FeatureX.Step0"))
            });

            Assert.AreEqual(TutorialStartResult.PrerequisiteNotMet, first.Start(new TutorialId("FeatureX")));

            first.Start(new TutorialId("Onboarding"));
            first.CompleteCurrentStep();
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry);
            TutorialDefinition onboarding2 = TrackedTutorial("Onboarding");
            second.RegisterTutorial(onboarding2, new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("Onboarding.Step0-2"), new InstructionStep("Onboarding.Step0"))
            });
            TutorialDefinition feature2 = TrackedTutorial("FeatureX", prerequisites: new[] { "Onboarding" });
            second.RegisterTutorial(feature2, new List<TutorialStepEntry>
            {
                new TutorialStepEntry(TrackedStep("FeatureX.Step0-2"), new InstructionStep("FeatureX.Step0"))
            });

            Assert.AreEqual(TutorialStartResult.Success, second.Start(new TutorialId("FeatureX")));

            second.Shutdown();
        }

        private readonly struct CarStartedMovingEvent
        {
        }
    }
}
