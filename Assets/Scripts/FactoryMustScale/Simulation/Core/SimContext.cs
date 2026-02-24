namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Shared per-tick simulation context.
    ///
    /// Contract:
    /// - PreCompute(A) and Compute can read authoritative state and write transient buffers only.
    /// - PreCompute(B) may apply structural cell edits only (build/remove/rotate).
    /// - Commit applies non-structural authoritative mutations.
    /// </summary>
    public struct SimContext
    {
        private readonly int[] _phaseTraceBuffer;
        private readonly ISimHashSource[] _hashSources;
        private readonly int _hashSourceCount;

        public SimContext(in SimClock clock, int[] phaseTraceBuffer, ISimHashSource[] hashSources, int hashSourceCount)
        {
            Clock = clock;
            _phaseTraceBuffer = phaseTraceBuffer;
            _hashSources = hashSources;
            _hashSourceCount = hashSourceCount;
            PhaseTraceCount = 0;
        }

        public SimClock Clock { get; }

        public int HashSourceCount => _hashSourceCount;

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

        public bool TryGetHashSource(int index, out ISimHashSource source)
        {
            if (_hashSources == null || index < 0 || index >= _hashSourceCount)
            {
                source = null;
                return false;
            }

            source = _hashSources[index];
            return source != null;
        }
    }

    public enum SimPhase : byte
    {
        PreCompute = 0,
        Compute = 1,
        Commit = 2,
    }
}
