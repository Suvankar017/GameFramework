using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>Covers <see cref="TutorialPersistencePolicy"/> behavior, the registration-after-Load
    /// ordering the framework already hit once in Phase 6 (see CLAUDE.md's Phase 9 brief, section
    /// 26), and graceful handling of corrupted persisted data.</summary>
    public class TutorialPersistenceTests
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

        private TutorialDefinition RegisterOneStepTutorial(
            TutorialService service, string id, TutorialPersistencePolicy policy, TutorialRepeatPolicy repeatPolicy = TutorialRepeatPolicy.Once, int stepCount = 1)
        {
            TutorialDefinition definition = TestDefinitions.Tutorial(id, persistencePolicy: policy, repeatPolicy: repeatPolicy);
            _createdAssets.Add(definition);

            var steps = new List<TutorialStepEntry>();
            for (int i = 0; i < stepCount; i++)
            {
                TutorialStepDefinition stepDefinition = TestDefinitions.Step($"{id}.Step{i}");
                _createdAssets.Add(stepDefinition);
                steps.Add(new TutorialStepEntry(stepDefinition, new InstructionStep($"{id}.Step{i}")));
            }

            service.RegisterTutorial(definition, steps);
            return definition;
        }

        [Test]
        public void CompletionOnly_SurvivesSaveAndLoad_OnANewServiceInstance()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.CompletionOnly);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep();
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry); // re-runs Load() against the same PersistenceService
            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.CompletionOnly);

            Assert.IsTrue(second.HasCompleted(new TutorialId("A")));
            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, second.Start(new TutorialId("A")));
            second.Shutdown();
        }

        [Test]
        public void None_Policy_NeverPersists_EvenAfterCompletion()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.None);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep();
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry);
            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.None);

            Assert.IsFalse(second.HasCompleted(new TutorialId("A")));
            second.Shutdown();
        }

        [Test]
        public void ResumeProgress_InterruptedRun_ResumesFromPersistedStep_OnNextStart()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.ResumeProgress, stepCount: 3);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep(); // now on Step1 - never completes Step1, simulating a crash
            first.Save();
            first.Shutdown(); // does not mark HasCompleted; ResumeStepIndex(1) was already snapshotted at BeginStep

            var second = new TutorialService();
            second.Initialize(registry);
            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.ResumeProgress, stepCount: 3);

            second.Start(new TutorialId("A"));

            Assert.AreEqual("A.Step1", second.ActiveStepId);
            second.Shutdown();
        }

        [Test]
        public void CompletionOnly_InterruptedRun_RestartsFromFirstStep_OnNextStart()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.CompletionOnly, stepCount: 3);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep(); // now on Step1
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry);
            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.CompletionOnly, stepCount: 3);

            second.Start(new TutorialId("A"));

            Assert.AreEqual("A.Step0", second.ActiveStepId);
            second.Shutdown();
        }

        [Test]
        public void RegistrationAfterLoad_StillAppliesPersistedCompletion()
        {
            // Mirrors the Phase 6 bug this framework already hit once: RegisterTutorial normally
            // runs after Initialize()/Load(), so persisted state must be staged and applied when
            // registration finally happens, not discarded because _tutorials was empty at Load time.
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.CompletionOnly);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep();
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry); // Load() runs now, before any tutorial is registered

            Assert.IsFalse(second.IsRegistered(new TutorialId("A")));

            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.CompletionOnly); // applies staged persisted state

            Assert.IsTrue(second.HasCompleted(new TutorialId("A")));
            second.Shutdown();
        }

        [Test]
        public void OncePerSession_AllowsAStart_InANewSession_DespitePersistedCompletion()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            var first = new TutorialService();
            first.Initialize(registry);
            RegisterOneStepTutorial(first, "A", TutorialPersistencePolicy.CompletionOnly, TutorialRepeatPolicy.OncePerSession);
            first.Start(new TutorialId("A"));
            first.CompleteCurrentStep();
            Assert.AreEqual(TutorialStartResult.AlreadyCompleted, first.Start(new TutorialId("A")));
            first.Save();
            first.Shutdown();

            var second = new TutorialService();
            second.Initialize(registry);
            RegisterOneStepTutorial(second, "A", TutorialPersistencePolicy.CompletionOnly, TutorialRepeatPolicy.OncePerSession);

            TutorialStartResult result = second.Start(new TutorialId("A"));

            Assert.AreEqual(TutorialStartResult.Success, result, "A fresh session has no in-memory completedThisSession record, so OncePerSession must allow starting again even though HasCompleted is still true from the saved file.");
            second.Shutdown();
        }

        [Test]
        public void CorruptedSaveData_MismatchedColumnLengths_IsIgnoredWithoutCrashing()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out PersistenceService persistence);
            persistence.Save("GameFramework.Tutorials", new BrokenTutorialSaveData(), 1);

            var service = new TutorialService();
            Assert.DoesNotThrow(() => service.Initialize(registry));
            RegisterOneStepTutorial(service, "A", TutorialPersistencePolicy.CompletionOnly);

            Assert.IsFalse(service.HasCompleted(new TutorialId("A")));
            service.Shutdown();
        }

        [System.Serializable]
        private sealed class BrokenTutorialSaveData
        {
            public List<string> TutorialIds = new List<string> { "A", "B" };
            public List<int> Completed = new List<int> { 1 }; // deliberately shorter than TutorialIds
            public List<int> Skipped = new List<int> { 0 };
            public List<int> ResumeStepIndices = new List<int> { -1 };
        }
    }
}
