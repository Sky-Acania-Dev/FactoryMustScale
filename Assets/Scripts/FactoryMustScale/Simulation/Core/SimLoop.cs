using System;

namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Runs the canonical 3-phase simulation loop in deterministic system index order.
    /// </summary>
    public sealed class SimLoop
    {
        public const int HashHistorySize = 256;

        private readonly ISimSystem[] _systems;
        private readonly ISimHashSource[] _hashSources;
        private readonly int[] _hashTickBuffer;
        private readonly ulong[] _hashValueBuffer;
        private readonly int[] _phaseTraceBuffer;
        private int _unitTick;
        private int _phaseTraceCount;
        private int _hashWriteIndex;
        private int _hashCount;

        public static bool EnableHashChecks { get; set; }

        public SimLoop(ISimSystem[] systems, int traceCapacity = 1024)
        {
            if (systems == null)
            {
                throw new ArgumentNullException(nameof(systems));
            }

            if (traceCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(traceCapacity));
            }

            _systems = systems;
            _hashSources = new ISimHashSource[systems.Length];
            int hashSourceCount = 0;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] is ISimHashSource hashSource)
                {
                    _hashSources[hashSourceCount] = hashSource;
                    hashSourceCount++;
                }
            }

            _phaseTraceBuffer = traceCapacity == 0 ? Array.Empty<int>() : new int[traceCapacity];
            _hashTickBuffer = new int[HashHistorySize];
            _hashValueBuffer = new ulong[HashHistorySize];
            _unitTick = 0;
            _phaseTraceCount = 0;
            HashSourceCount = hashSourceCount;
            _hashWriteIndex = 0;
            _hashCount = 0;
        }

        public int UnitTick => _unitTick;
        public int HashSourceCount { get; }

        public int PhaseTraceCount => _phaseTraceCount;

        public int HashRecordCount => _hashCount;

        public bool TryGetPhaseTraceAt(int index, out int traceValue)
        {
            if (index < 0 || index >= _phaseTraceCount)
            {
                traceValue = 0;
                return false;
            }

            traceValue = _phaseTraceBuffer[index];
            return true;
        }

        public void Tick()
        {
            Tick(new SimClock(_unitTick + 1));
        }

        public void Tick(in SimClock clock)
        {
            _unitTick = clock.UnitTick;

            SimContext ctx = new SimContext(in clock, _phaseTraceBuffer, _hashSources, HashSourceCount);

            ExecutePhase(ref ctx, SimPhase.PreCompute);
            ExecutePhase(ref ctx, SimPhase.Compute);
            ExecutePhase(ref ctx, SimPhase.Commit);
            TryRecordSimHash(in ctx);

            _phaseTraceCount = ctx.PhaseTraceCount;
        }

        public bool TryGetHashRecordAt(int index, out int tickIndex, out ulong hashValue)
        {
            if (index < 0 || index >= _hashCount)
            {
                tickIndex = 0;
                hashValue = 0UL;
                return false;
            }

            int oldestIndex = _hashCount == HashHistorySize ? _hashWriteIndex : 0;
            int ringIndex = (oldestIndex + index) % HashHistorySize;
            tickIndex = _hashTickBuffer[ringIndex];
            hashValue = _hashValueBuffer[ringIndex];
            return true;
        }

        private void ExecutePhase(ref SimContext ctx, SimPhase phase)
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                switch (phase)
                {
                    case SimPhase.PreCompute:
                        _systems[i].PreCompute(ref ctx);
                        break;
                    case SimPhase.Compute:
                        _systems[i].Compute(ref ctx);
                        break;
                    default:
                        _systems[i].Commit(ref ctx);
                        break;
                }

                ctx.AppendPhaseTrace(phase);
            }
        }

        private void TryRecordSimHash(in SimContext context)
        {
            if (!EnableHashChecks)
            {
                return;
            }

            ulong hash = SimHash.ComputeHash(in context);
            _hashTickBuffer[_hashWriteIndex] = context.Clock.UnitTick;
            _hashValueBuffer[_hashWriteIndex] = hash;
            _hashWriteIndex = (_hashWriteIndex + 1) % HashHistorySize;

            if (_hashCount < HashHistorySize)
            {
                _hashCount++;
            }
        }
    }
}
