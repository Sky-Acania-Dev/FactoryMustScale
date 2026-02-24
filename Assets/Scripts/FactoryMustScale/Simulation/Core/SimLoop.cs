using System;

namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Runs the canonical 3-phase simulation loop in deterministic system index order.
    /// </summary>
    public sealed class SimLoop
    {
        private readonly ISimSystem[] _systems;
        private readonly int[] _phaseTraceBuffer;
        private int _unitTick;
        private int _phaseTraceCount;

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
            _phaseTraceBuffer = traceCapacity == 0 ? Array.Empty<int>() : new int[traceCapacity];
            _unitTick = 0;
            _phaseTraceCount = 0;
        }

        public int UnitTick => _unitTick;

        public int PhaseTraceCount => _phaseTraceCount;

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

            SimContext ctx = new SimContext(in clock, _phaseTraceBuffer);

            ExecutePhase(ref ctx, SimPhase.PreCompute);
            ExecutePhase(ref ctx, SimPhase.Compute);
            ExecutePhase(ref ctx, SimPhase.Commit);

            _phaseTraceCount = ctx.PhaseTraceCount;
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
    }
}
