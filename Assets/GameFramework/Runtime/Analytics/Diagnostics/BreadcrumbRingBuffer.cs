using System;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// Fixed-capacity circular buffer of the most recent <see cref="Breadcrumb"/>s - see CLAUDE.md's
    /// Phase 16 brief, section 31 ("avoid storing unlimited history... use a bounded ring buffer or
    /// similarly simple bounded structure"). <see cref="Add"/> is O(1) and allocation-free;
    /// <see cref="ToArray"/> allocates a snapshot array in chronological order and is only called
    /// when a report is actually being built (not a hot path).
    /// </summary>
    public sealed class BreadcrumbRingBuffer
    {
        private readonly Breadcrumb[] _buffer;
        private int _head;
        private int _count;

        public BreadcrumbRingBuffer(int capacity)
        {
            _buffer = new Breadcrumb[Math.Max(1, capacity)];
        }

        public int Count => _count;

        public void Add(Breadcrumb breadcrumb)
        {
            _buffer[_head] = breadcrumb;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
            {
                _count++;
            }
        }

        public Breadcrumb[] ToArray()
        {
            var result = new Breadcrumb[_count];
            int start = _count < _buffer.Length ? 0 : _head;

            for (int i = 0; i < _count; i++)
            {
                result[i] = _buffer[(start + i) % _buffer.Length];
            }

            return result;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
