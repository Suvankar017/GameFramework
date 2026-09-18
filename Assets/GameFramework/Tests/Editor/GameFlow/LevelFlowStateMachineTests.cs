using NUnit.Framework;

namespace GameFramework.GameFlow.Tests
{
    public class LevelFlowStateMachineTests
    {
        [Test]
        public void InitialState_IsUnloaded()
        {
            var machine = new LevelFlowStateMachine();

            Assert.AreEqual(LevelFlowState.Unloaded, machine.Current);
            Assert.AreEqual(1, machine.History.Count);
            Assert.AreEqual(LevelFlowState.Unloaded, machine.History[0]);
        }

        [Test]
        public void TryTransition_ToAllowedTarget_Succeeds()
        {
            var machine = new LevelFlowStateMachine();

            TransitionResult result = machine.TryTransition(LevelFlowState.Loading);

            Assert.AreEqual(TransitionResult.Success, result);
            Assert.AreEqual(LevelFlowState.Loading, machine.Current);
        }

        [Test]
        public void TryTransition_ToDisallowedTarget_ReturnsInvalidTransition_AndDoesNotChangeState()
        {
            var machine = new LevelFlowStateMachine();

            TransitionResult result = machine.TryTransition(LevelFlowState.Playing);

            Assert.AreEqual(TransitionResult.InvalidTransition, result);
            Assert.AreEqual(LevelFlowState.Unloaded, machine.Current);
        }

        [Test]
        public void TryTransition_ToCurrentState_ReturnsAlreadyInState()
        {
            var machine = new LevelFlowStateMachine();

            TransitionResult result = machine.TryTransition(LevelFlowState.Unloaded);

            Assert.AreEqual(TransitionResult.AlreadyInState, result);
        }

        [Test]
        public void StateChanged_FiresWithPreviousAndCurrent()
        {
            var machine = new LevelFlowStateMachine();
            LevelFlowState observedPrevious = default;
            LevelFlowState observedCurrent = default;
            int callCount = 0;

            machine.StateChanged += (previous, current) =>
            {
                observedPrevious = previous;
                observedCurrent = current;
                callCount++;
            };

            machine.TryTransition(LevelFlowState.Loading);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(LevelFlowState.Unloaded, observedPrevious);
            Assert.AreEqual(LevelFlowState.Loading, observedCurrent);
        }

        [Test]
        public void FullHappyPathSequence_EachStepSucceeds()
        {
            var machine = new LevelFlowStateMachine();

            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Loading));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Initializing));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Ready));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Playing));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Paused));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Playing));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Completing));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Completed));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Exiting));
            Assert.AreEqual(TransitionResult.Success, machine.TryTransition(LevelFlowState.Unloaded));
        }

        [Test]
        public void Failed_CanTransitionDirectlyToPlaying_ForRespawn()
        {
            var machine = new LevelFlowStateMachine();
            machine.TryTransition(LevelFlowState.Loading);
            machine.TryTransition(LevelFlowState.Initializing);
            machine.TryTransition(LevelFlowState.Ready);
            machine.TryTransition(LevelFlowState.Playing);
            machine.TryTransition(LevelFlowState.Failing);
            machine.TryTransition(LevelFlowState.Failed);

            TransitionResult result = machine.TryTransition(LevelFlowState.Playing);

            Assert.AreEqual(TransitionResult.Success, result);
        }

        [Test]
        public void Completed_CannotTransitionBackToPlaying_WithoutRestarting()
        {
            var machine = new LevelFlowStateMachine();
            machine.TryTransition(LevelFlowState.Loading);
            machine.TryTransition(LevelFlowState.Initializing);
            machine.TryTransition(LevelFlowState.Ready);
            machine.TryTransition(LevelFlowState.Playing);
            machine.TryTransition(LevelFlowState.Completing);
            machine.TryTransition(LevelFlowState.Completed);

            TransitionResult result = machine.TryTransition(LevelFlowState.Playing);

            Assert.AreEqual(TransitionResult.InvalidTransition, result);
        }

        [Test]
        public void History_IsBounded()
        {
            var machine = new LevelFlowStateMachine();

            // Cycle Ready<->Playing... actually simplest bounded cycle: Loading/Exiting isn't
            // reversible, so drive Playing<->Paused repeatedly, which is a legal cycle.
            machine.TryTransition(LevelFlowState.Loading);
            machine.TryTransition(LevelFlowState.Initializing);
            machine.TryTransition(LevelFlowState.Ready);
            machine.TryTransition(LevelFlowState.Playing);

            for (int i = 0; i < 40; i++)
            {
                machine.TryTransition(LevelFlowState.Paused);
                machine.TryTransition(LevelFlowState.Playing);
            }

            Assert.LessOrEqual(machine.History.Count, 32);
        }
    }
}
