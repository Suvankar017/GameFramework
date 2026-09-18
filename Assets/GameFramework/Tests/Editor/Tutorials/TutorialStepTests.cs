using GameFramework.Input;
using GameFramework.Runtime.Events;
using GameFramework.Tutorials.Conditions;
using GameFramework.Tutorials.Steps;
using NUnit.Framework;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>Unit tests for each built-in <see cref="ITutorialStep"/> in isolation - no
    /// <see cref="TutorialService"/> involved, matching CLAUDE.md's Phase 9 brief's call for
    /// independently testable step logic.</summary>
    public class TutorialStepTests
    {
        [Test]
        public void TutorialStepBase_Begin_TransitionsNotStartedToActive()
        {
            var step = new InstructionStep("S1");

            step.Begin();

            Assert.AreEqual(TutorialStepState.Active, step.State);
        }

        [Test]
        public void TutorialStepBase_Begin_CalledTwice_SecondCallIsIgnored()
        {
            var step = new InstructionStep("S1");
            step.Begin();

            step.Begin();

            Assert.AreEqual(TutorialStepState.Active, step.State);
        }

        [Test]
        public void TutorialStepBase_Complete_RaisesCompletedExactlyOnce_EvenIfCalledTwice()
        {
            var step = new InstructionStep("S1");
            int callCount = 0;
            step.Completed += () => callCount++;
            step.Begin();

            step.Complete();
            step.Complete();

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void TutorialStepBase_Complete_BeforeBegin_IsIgnored_NoEventRaised()
        {
            var step = new InstructionStep("S1");
            bool raised = false;
            step.Completed += () => raised = true;

            step.Complete();

            Assert.IsFalse(raised);
            Assert.AreEqual(TutorialStepState.NotStarted, step.State);
        }

        [Test]
        public void TutorialStepBase_Cancel_FromActive_DoesNotRaiseCompleted()
        {
            var step = new InstructionStep("S1");
            bool raised = false;
            step.Completed += () => raised = true;
            step.Begin();

            step.Cancel();

            Assert.IsFalse(raised);
            Assert.AreEqual(TutorialStepState.Cancelled, step.State);
        }

        [Test]
        public void TutorialStepBase_Cancel_AfterComplete_IsIgnored()
        {
            var step = new InstructionStep("S1");
            step.Begin();
            step.Complete();

            step.Cancel();

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void TutorialStepBase_Reset_AfterComplete_AllowsBeginAgain()
        {
            var step = new InstructionStep("S1");
            step.Begin();
            step.Complete();

            step.Reset();

            Assert.AreEqual(TutorialStepState.NotStarted, step.State);
            step.Begin();
            Assert.AreEqual(TutorialStepState.Active, step.State);
        }

        [Test]
        public void TutorialStepBase_Reset_AfterCancel_AllowsBeginAgain()
        {
            var step = new InstructionStep("S1");
            step.Begin();
            step.Cancel();

            step.Reset();

            Assert.AreEqual(TutorialStepState.NotStarted, step.State);
            step.Begin();
            Assert.AreEqual(TutorialStepState.Active, step.State);
        }

        [Test]
        public void TutorialStepBase_Reset_WhileNotStarted_IsANoOp()
        {
            var step = new InstructionStep("S1");

            step.Reset();

            Assert.AreEqual(TutorialStepState.NotStarted, step.State);
        }

        [Test]
        public void WaitStep_Reset_ThenBegin_TimesFromZeroAgain()
        {
            var step = new WaitStep("S1", 2f);
            step.Begin();
            step.Tick(2f);
            Assert.AreEqual(TutorialStepState.Completed, step.State);

            step.Reset();
            step.Begin();
            step.Tick(1f);

            Assert.AreEqual(TutorialStepState.Active, step.State, "A reset WaitStep must not retain elapsed time from its previous run.");
        }

        [Test]
        public void InstructionStep_WithNoAutoAdvance_OnlyCompletesManually()
        {
            var step = new InstructionStep("S1");
            step.Begin();

            step.Tick(1000f);
            Assert.AreEqual(TutorialStepState.Active, step.State);

            step.Complete();
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void InstructionStep_WithAutoAdvance_CompletesAfterDelayElapses()
        {
            var step = new InstructionStep("S1", autoAdvanceDelaySeconds: 2f);
            step.Begin();

            step.Tick(1f);
            Assert.AreEqual(TutorialStepState.Active, step.State);

            step.Tick(1f);
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void WaitStep_ZeroDuration_CompletesImmediatelyOnBegin()
        {
            var step = new WaitStep("S1", 0f);

            step.Begin();

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void WaitStep_PositiveDuration_CompletesOnceElapsed()
        {
            var step = new WaitStep("S1", 3f);
            step.Begin();

            step.Tick(2f);
            Assert.AreEqual(TutorialStepState.Active, step.State);

            step.Tick(1f);
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void InputStep_Pressed_CompletesOnWasPressedThisFrame()
        {
            var input = new FakeInputService();
            var step = new InputStep("S1", input, "Jump", InputTriggerType.Pressed);
            step.Begin();

            input.SetState("Jump", new InputActionState(true, true, false, 0f, UnityEngine.Vector2.zero));
            step.Tick(0f);

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void InputStep_Pressed_IgnoresUnrelatedAction()
        {
            var input = new FakeInputService();
            var step = new InputStep("S1", input, "Jump", InputTriggerType.Pressed);
            step.Begin();

            input.SetState("Attack", new InputActionState(true, true, false, 0f, UnityEngine.Vector2.zero));
            step.Tick(0f);

            Assert.AreEqual(TutorialStepState.Active, step.State);
        }

        [Test]
        public void InputStep_Held_CompletesWhileActionIsPressed()
        {
            var input = new FakeInputService();
            var step = new InputStep("S1", input, "Move", InputTriggerType.Held);
            step.Begin();

            input.SetState("Move", new InputActionState(true, false, false, 0f, UnityEngine.Vector2.zero));
            step.Tick(0f);

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void InputStep_Released_CompletesOnWasReleasedThisFrame()
        {
            var input = new FakeInputService();
            var step = new InputStep("S1", input, "Jump", InputTriggerType.Released);
            step.Begin();

            input.SetState("Jump", new InputActionState(false, false, true, 0f, UnityEngine.Vector2.zero));
            step.Tick(0f);

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void InputStep_RespectsInputContextGating()
        {
            var input = new FakeInputService();
            input.PushContext(InputContextDefinition.Restricted("Menu", "Confirm"));
            var step = new InputStep("S1", input, "Jump", InputTriggerType.Pressed);
            step.Begin();

            input.SetState("Jump", new InputActionState(true, true, false, 0f, UnityEngine.Vector2.zero));
            step.Tick(0f);

            Assert.AreEqual(TutorialStepState.Active, step.State, "Jump is not allowed by the current context, so the sampled state must read as None.");
        }

        [Test]
        public void EventStep_CompletesOnMatchingEvent()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out _);
            var step = new EventStep<TestPayloadEvent>("S1", events);
            step.Begin();

            events.Publish(new TestPayloadEvent(1));

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void EventStep_WithPredicate_IgnoresNonMatchingPayload()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out _);
            var step = new EventStep<TestPayloadEvent>("S1", events, e => e.Value >= 10);
            step.Begin();

            events.Publish(new TestPayloadEvent(1));
            Assert.AreEqual(TutorialStepState.Active, step.State);

            events.Publish(new TestPayloadEvent(10));
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void EventStep_UnsubscribesOnCancel_SoALaterEventCannotCompleteIt()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out EventService events, out _);
            var step = new EventStep<TestPayloadEvent>("S1", events);
            step.Begin();
            step.Cancel();

            events.Publish(new TestPayloadEvent(1));

            Assert.AreEqual(TutorialStepState.Cancelled, step.State);
        }

        [Test]
        public void ConditionStep_AlreadySatisfied_CompletesOnBegin()
        {
            var condition = new DelegateTutorialCondition(() => true);
            var step = new ConditionStep("S1", condition);

            step.Begin();

            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void ConditionStep_BecomesSatisfied_CompletesOnTick()
        {
            bool satisfied = false;
            var condition = new DelegateTutorialCondition(() => satisfied);
            var step = new ConditionStep("S1", condition);
            step.Begin();

            step.Tick(0f);
            Assert.AreEqual(TutorialStepState.Active, step.State);

            satisfied = true;
            step.Tick(0f);
            Assert.AreEqual(TutorialStepState.Completed, step.State);
        }

        [Test]
        public void DelegateTutorialCondition_Describe_ReturnsSuppliedDescription()
        {
            var condition = new DelegateTutorialCondition(() => true, "Car is moving");

            Assert.AreEqual("Car is moving", condition.Describe());
        }

        private readonly struct TestPayloadEvent
        {
            public readonly int Value;
            public TestPayloadEvent(int value) => Value = value;
        }
    }
}
