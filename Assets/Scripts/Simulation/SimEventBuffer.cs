using System;
using System.Text;

namespace FactoryMustScale.Simulation
{
    public struct SimEventBuffer
    {
        private SimEvent[] _workingEvents;
        private int _workingCount;
        private SimEvent[] _nextEvents;
        private int _nextCount;
        private SimEvent[] _tickEvents;
        private int _tickCount;
        private SimEvent[] _history;
        private int _historyCount;
        private bool _overflowed;

        public int WorkingCount => _workingCount;
        public int TickCount => _tickCount;
        public int HistoryCount => _historyCount;
        public bool Overflowed => _overflowed;

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                capacity = 1;
            }

            EnsureArray(ref _workingEvents, capacity);
            EnsureArray(ref _nextEvents, capacity);
            EnsureArray(ref _tickEvents, capacity);
            EnsureArray(ref _history, checked(capacity * 16));
        }

        public void BeginTick()
        {
            _tickCount = 0;
            _overflowed = false;
        }

        public void PromoteQueuedEvents()
        {
            _workingCount = _nextCount;
            for (int i = 0; i < _nextCount; i++)
            {
                _workingEvents[i] = _nextEvents[i];
            }

            _nextCount = 0;
        }

        public bool TryGetWorkingEvent(int index, out SimEvent simEvent)
        {
            if (index < 0 || index >= _workingCount)
            {
                simEvent = default;
                return false;
            }

            simEvent = _workingEvents[index];
            return true;
        }

        public bool QueueForNextTick(SimEvent simEvent)
        {
            if (!TryAppend(ref _nextEvents, ref _nextCount, simEvent))
            {
                return false;
            }

            return true;
        }

        public bool RecordApplied(SimEvent simEvent)
        {
            if (!TryAppend(ref _tickEvents, ref _tickCount, simEvent))
            {
                return false;
            }

            return TryAppend(ref _history, ref _historyCount, simEvent);
        }

        public bool TryGetHistoryEvent(int index, out SimEvent simEvent)
        {
            if (index < 0 || index >= _historyCount)
            {
                simEvent = default;
                return false;
            }

            simEvent = _history[index];
            return true;
        }

        public string DumpTickEvents()
        {
            if (_tickCount == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _tickCount; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(_tickEvents[i].ToString());
            }

            return builder.ToString();
        }

        private static void EnsureArray(ref SimEvent[] buffer, int capacity)
        {
            if (buffer == null || buffer.Length != capacity)
            {
                buffer = new SimEvent[capacity];
            }
        }

        private bool TryAppend(ref SimEvent[] buffer, ref int count, SimEvent simEvent)
        {
            if (buffer == null || count >= buffer.Length)
            {
                _overflowed = true;
                return false;
            }

            buffer[count] = simEvent;
            count++;
            return true;
        }
    }
}
