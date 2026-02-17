using System;

namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Preallocated deterministic event buffer used by the two-phase simulation pipeline.
    /// </summary>
    public struct EventBuffer
    {
        public struct EventRecord
        {
            public int TargetIndex;
            public int OpCode;
            public int A;
            public int B;
            public int SourceIndex;
        }

        private readonly EventRecord[] _events;
        private int _count;
        private bool _overflowed;

        public EventBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _events = new EventRecord[capacity];
            _count = 0;
            _overflowed = false;
        }

        public int Capacity => _events.Length;
        public int Count => _count;
        public bool Overflowed => _overflowed;

        public void Clear()
        {
            _count = 0;
            _overflowed = false;
        }

        public bool Append(EventRecord record)
        {
            if (_count >= _events.Length)
            {
                _overflowed = true;
                return false;
            }

            _events[_count] = record;
            _count++;
            return true;
        }

        public bool TryGetAt(int index, out EventRecord record)
        {
            if (index < 0 || index >= _count)
            {
                record = default;
                return false;
            }

            record = _events[index];
            return true;
        }
    }
}
