using System;
using System.Collections.Generic;

namespace GameFramework.Performance.Ticking
{
    /// <summary>
    /// Internal registration list shared by <see cref="TickService"/> for each of its three phases.
    /// Sorted-on-insert by priority (ascending - lower ticks first) rather than sorted every frame,
    /// since registration is far less frequent than ticking. Duplicate Register/Unregister calls
    /// are safe no-ops, matching <see cref="Runtime.Events.IEventService"/>'s subscribe semantics.
    /// </summary>
    internal sealed class TickRegistry<T> where T : class
    {
        private readonly List<T> _entries = new List<T>();
        private readonly List<int> _priorities = new List<int>();
        private readonly List<TickGroup> _groups = new List<TickGroup>();
        private T[] _snapshot = Array.Empty<T>();

        public int Count => _entries.Count;

        public void Register(T tickable, int priority, TickGroup group)
        {
            if (tickable == null || _entries.Contains(tickable))
            {
                return;
            }

            int index = _entries.Count;
            while (index > 0 && _priorities[index - 1] > priority)
            {
                index--;
            }

            _entries.Insert(index, tickable);
            _priorities.Insert(index, priority);
            _groups.Insert(index, group);
        }

        public void Unregister(T tickable)
        {
            int index = _entries.IndexOf(tickable);
            if (index < 0)
            {
                return;
            }

            _entries.RemoveAt(index);
            _priorities.RemoveAt(index);
            _groups.RemoveAt(index);
        }

        public int CountInGroup(TickGroup group)
        {
            int count = 0;
            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i] == group)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the live registration list into a reused, geometrically-grown buffer and returns
        /// it - allocation-free once the buffer has grown to cover the peak count. Register/Unregister
        /// calls made while the caller iterates the returned buffer only mutate the live list; they
        /// are picked up by the next call to <see cref="Snapshot"/>, never the one already handed
        /// out, so modifying tick registration from inside a tick callback can never corrupt the
        /// in-progress iteration.
        /// </summary>
        public T[] Snapshot(out int count)
        {
            count = _entries.Count;
            if (_snapshot.Length < count)
            {
                int newSize = Math.Max(4, _snapshot.Length * 2);
                while (newSize < count)
                {
                    newSize *= 2;
                }

                _snapshot = new T[newSize];
            }

            _entries.CopyTo(_snapshot);
            return _snapshot;
        }
    }
}
