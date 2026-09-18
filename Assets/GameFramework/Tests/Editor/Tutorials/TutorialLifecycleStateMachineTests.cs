using NUnit.Framework;

namespace GameFramework.Tutorials.Tests
{
    public class TutorialLifecycleStateMachineTests
    {
        [Test]
        public void InitialState_IsInactive()
        {
            var machine = new TutorialLifecycleStateMachine();

            Assert.AreEqual(TutorialState.Inactive, machine.Current);
            Assert.AreEqual(1, machine.History.Count);
            Assert.AreEqual(TutorialState.Inactive, machine.History[0]);
        }

        [Test]
        public void TryTransition_ToAllowedTarget_Succeeds()
        {
            var machine = new TutorialLifecycleStateMachine();

            bool result = machine.TryTransition(TutorialState.Starting);

            Assert.IsTrue(result);
            Assert.AreEqual(TutorialState.Starting, machine.Current);
        }

        [Test]
        public void TryTransition_ToDisallowedTarget_Fails_AndDoesNotChangeState()
        {
            var machine = new TutorialLifecycleStateMachine();

            bool result = machine.TryTransition(TutorialState.Running);

            Assert.IsFalse(result);
            Assert.AreEqual(TutorialState.Inactive, machine.Current);
        }

        [Test]
        public void TryTransition_ToCurrentState_Fails()
        {
            var machine = new TutorialLifecycleStateMachine();

            bool result = machine.TryTransition(TutorialState.Inactive);

            Assert.IsFalse(result);
        }

        [Test]
        public void StateChanged_FiresWithPreviousAndCurrent()
        {
            var machine = new TutorialLifecycleStateMachine();
            TutorialState observedPrevious = default;
            TutorialState observedCurrent = default;
            int callCount = 0;

            machine.StateChanged += (previous, current) =>
            {
                observedPrevious = previous;
                observedCurrent = current;
                callCount++;
            };

            machine.TryTransition(TutorialState.Starting);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(TutorialState.Inactive, observedPrevious);
            Assert.AreEqual(TutorialState.Starting, observedCurrent);
        }

        [Test]
        public void FullHappyPathSequence_EachStepSucceeds()
        {
            var machine = new TutorialLifecycleStateMachine();

            Assert.IsTrue(machine.TryTransition(TutorialState.Starting));
            Assert.IsTrue(machine.TryTransition(TutorialState.Running));
            Assert.IsTrue(machine.TryTransition(TutorialState.Paused));
            Assert.IsTrue(machine.TryTransition(TutorialState.Running));
            Assert.IsTrue(machine.TryTransition(TutorialState.Completing));
            Assert.IsTrue(machine.TryTransition(TutorialState.Completed));
            Assert.IsTrue(machine.TryTransition(TutorialState.Inactive));
        }

        [Test]
        public void CancelPath_FromPaused_Succeeds()
        {
            var machine = new TutorialLifecycleStateMachine();
            machine.TryTransition(TutorialState.Starting);
            machine.TryTransition(TutorialState.Running);
            machine.TryTransition(TutorialState.Paused);

            Assert.IsTrue(machine.TryTransition(TutorialState.Cancelling));
            Assert.IsTrue(machine.TryTransition(TutorialState.Cancelled));
            Assert.IsTrue(machine.TryTransition(TutorialState.Inactive));
        }

        [Test]
        public void Completed_CanTransitionDirectlyToStarting_ForRestart()
        {
            var machine = new TutorialLifecycleStateMachine();
            machine.TryTransition(TutorialState.Starting);
            machine.TryTransition(TutorialState.Running);
            machine.TryTransition(TutorialState.Completing);
            machine.TryTransition(TutorialState.Completed);

            Assert.IsTrue(machine.TryTransition(TutorialState.Starting));
        }

        [Test]
        public void Cancelled_CanTransitionDirectlyToStarting_ForRestart()
        {
            var machine = new TutorialLifecycleStateMachine();
            machine.TryTransition(TutorialState.Starting);
            machine.TryTransition(TutorialState.Running);
            machine.TryTransition(TutorialState.Cancelling);
            machine.TryTransition(TutorialState.Cancelled);

            Assert.IsTrue(machine.TryTransition(TutorialState.Starting));
        }

        [Test]
        public void Completed_CannotTransitionBackToRunning_WithoutRestarting()
        {
            var machine = new TutorialLifecycleStateMachine();
            machine.TryTransition(TutorialState.Starting);
            machine.TryTransition(TutorialState.Running);
            machine.TryTransition(TutorialState.Completing);
            machine.TryTransition(TutorialState.Completed);

            Assert.IsFalse(machine.TryTransition(TutorialState.Running));
        }

        [Test]
        public void History_IsBounded()
        {
            var machine = new TutorialLifecycleStateMachine();
            machine.TryTransition(TutorialState.Starting);
            machine.TryTransition(TutorialState.Running);

            for (int i = 0; i < 40; i++)
            {
                machine.TryTransition(TutorialState.Paused);
                machine.TryTransition(TutorialState.Running);
            }

            Assert.LessOrEqual(machine.History.Count, 32);
        }
    }
}
