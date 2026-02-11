namespace FactoryMustScale.Simulation
{
    public enum FactoryCommandType : byte
    {
        None = 0,
        PlaceBuilding = 1,
        RemoveBuilding = 2,
        RotateBuilding = 3,
    }

    public struct FactoryCommand
    {
        public FactoryCommandType Type;
        public int X;
        public int Y;
        public int StateId;
        public int Orientation;
    }

    /// <summary>
    /// Preallocated deterministic command queue for one simulation tick.
    /// </summary>
    public struct FactoryCommandQueue
    {
        private FactoryCommand[] _commands;
        private int _count;

        public FactoryCommandQueue(int capacity)
        {
            _commands = capacity > 0 ? new FactoryCommand[capacity] : null;
            _count = 0;
        }

        public int Count => _count;

        public bool TryEnqueue(FactoryCommand command)
        {
            if (_commands == null || _count >= _commands.Length)
            {
                return false;
            }

            _commands[_count] = command;
            _count++;
            return true;
        }

        public FactoryCommand GetAt(int index)
        {
            return _commands[index];
        }

        public void Clear()
        {
            _count = 0;
        }
    }
}
