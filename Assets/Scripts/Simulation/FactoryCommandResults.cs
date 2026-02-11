namespace FactoryMustScale.Simulation
{
    public enum FactoryCommandFailureReason : byte
    {
        None = 0,
        UnknownCommand = 1,
        MissingBuildRule = 2,
        NotBuildable = 3,
        OutOfRange = 4,
        EmptyCell = 5,
        Unsupported = 6,
    }

    public struct FactoryCommandResult
    {
        public int CommandIndex;
        public FactoryCommandType CommandType;
        public int X;
        public int Y;
        public bool Success;
        public FactoryCommandFailureReason FailureReason;
        public int AppliedStateId;
    }

    /// <summary>
    /// Preallocated deterministic command result buffer for one simulation tick.
    /// </summary>
    public struct FactoryCommandResultBuffer
    {
        private FactoryCommandResult[] _results;
        private int _count;

        public FactoryCommandResultBuffer(int capacity)
        {
            _results = capacity > 0 ? new FactoryCommandResult[capacity] : null;
            _count = 0;
        }

        public int Count => _count;

        public bool TryAdd(FactoryCommandResult result)
        {
            if (_results == null || _count >= _results.Length)
            {
                return false;
            }

            _results[_count] = result;
            _count++;
            return true;
        }

        public FactoryCommandResult GetAt(int index)
        {
            return _results[index];
        }

        public void Clear()
        {
            _count = 0;
        }
    }
}
