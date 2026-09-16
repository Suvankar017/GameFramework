using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Gameplay.Commands
{
    /// <summary>
    /// Optional, explicitly-instantiated FIFO queue for gameplay commands that should run
    /// sequentially rather than immediately (e.g. a scripted/replay sequence). Not global and not
    /// auto-created — most commands should just be invoked directly through
    /// <see cref="GameplayCommandInvoker"/> with no added latency; only create one of these where a
    /// real deferred-execution need exists.
    /// </summary>
    public sealed class GameplayCommandQueue
    {
        private readonly Queue<IGameplayCommand> _queue = new Queue<IGameplayCommand>();

        public int Count => _queue.Count;

        public void Enqueue(IGameplayCommand command)
        {
            _queue.Enqueue(Guard.NotNull(command, nameof(command)));
        }

        /// <summary>Dequeues and invokes at most one command. Returns null if the queue is empty.
        /// The caller decides how often to call this (e.g. once per gameplay tick) — the queue adds
        /// no ticking or timing behavior of its own.</summary>
        public CommandResult? ProcessNext()
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            return GameplayCommandInvoker.Invoke(_queue.Dequeue());
        }

        public void Clear() => _queue.Clear();
    }
}
