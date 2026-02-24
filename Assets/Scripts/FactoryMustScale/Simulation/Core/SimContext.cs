namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Shared per-tick simulation context.
    ///
    /// Contract:
    /// - ExternalIngest and Compute can read authoritative state and write transient buffers only.
    /// - Commit is the only phase that may apply authoritative state mutations.
    /// </summary>
    public struct SimContext
    {
        private readonly int[] _phaseTraceBuffer;

        public SimContext(in SimClock clock, int[] phaseTraceBuffer)
        {
            Clock = clock;
            _phaseTraceBuffer = phaseTraceBuffer;
            PhaseTraceCount = 0;
        }

        public SimClock Clock { get; }

        public int PhaseTraceCount { get; private set; }

        public void AppendPhaseTrace(SimPhase phase)
        {
            if (_phaseTraceBuffer == null || PhaseTraceCount >= _phaseTraceBuffer.Length)
            {
                return;
            }

            _phaseTraceBuffer[PhaseTraceCount] = (Clock.UnitTick * 10) + (int)phase;
            PhaseTraceCount++;
        }

        public bool TryGetPhaseTraceAt(int index, out int traceValue)
        {
            if (_phaseTraceBuffer == null || index < 0 || index >= PhaseTraceCount)
            {
                traceValue = 0;
                return false;
            }

            traceValue = _phaseTraceBuffer[index];
            return true;
        }
    }

    public enum SimPhase : byte
    {
        ExternalIngest = 0,
        Compute = 1,
        Commit = 2,
    }
}
