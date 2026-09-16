using GameFramework.Gameplay.Commands;
using NUnit.Framework;

namespace GameFramework.Gameplay.Tests
{
    public class GameplayCommandTests
    {
        private sealed class FakeCommand : IGameplayCommand
        {
            public bool AllowExecute = true;
            public CommandResult ResultToReturn = CommandResult.Success();
            public int ExecuteCallCount;

            public bool CanExecute() => AllowExecute;

            public CommandResult Execute()
            {
                ExecuteCallCount++;
                return ResultToReturn;
            }
        }

        [Test]
        public void Invoke_CanExecuteTrue_CallsExecuteAndReturnsItsResult()
        {
            var command = new FakeCommand { ResultToReturn = CommandResult.Success("done") };

            CommandResult result = GameplayCommandInvoker.Invoke(command);

            Assert.AreEqual(1, command.ExecuteCallCount);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("done", result.Message);
        }

        [Test]
        public void Invoke_CanExecuteFalse_RejectsWithoutCallingExecute()
        {
            var command = new FakeCommand { AllowExecute = false };

            CommandResult result = GameplayCommandInvoker.Invoke(command);

            Assert.AreEqual(0, command.ExecuteCallCount);
            Assert.AreEqual(CommandResultStatus.Rejected, result.Status);
        }

        [Test]
        public void Invoke_NullCommand_ReturnsRejected()
        {
            CommandResult result = GameplayCommandInvoker.Invoke(null);

            Assert.AreEqual(CommandResultStatus.Rejected, result.Status);
        }

        [Test]
        public void Invoke_ExecuteReturnsFailure_PropagatesFailureStatus()
        {
            var command = new FakeCommand { ResultToReturn = CommandResult.Failure("nope") };

            CommandResult result = GameplayCommandInvoker.Invoke(command);

            Assert.AreEqual(CommandResultStatus.Failure, result.Status);
            Assert.AreEqual("nope", result.Message);
        }

        [Test]
        public void Invoke_CalledRepeatedly_ExecutesEachTime()
        {
            var command = new FakeCommand();

            GameplayCommandInvoker.Invoke(command);
            GameplayCommandInvoker.Invoke(command);
            GameplayCommandInvoker.Invoke(command);

            Assert.AreEqual(3, command.ExecuteCallCount);
        }

        [Test]
        public void Queue_ProcessNext_ExecutesInFifoOrder()
        {
            var first = new FakeCommand();
            var second = new FakeCommand();
            var queue = new GameplayCommandQueue();
            queue.Enqueue(first);
            queue.Enqueue(second);

            queue.ProcessNext();

            Assert.AreEqual(1, first.ExecuteCallCount);
            Assert.AreEqual(0, second.ExecuteCallCount);
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Queue_ProcessNext_EmptyQueue_ReturnsNull()
        {
            var queue = new GameplayCommandQueue();

            Assert.IsNull(queue.ProcessNext());
        }
    }
}
