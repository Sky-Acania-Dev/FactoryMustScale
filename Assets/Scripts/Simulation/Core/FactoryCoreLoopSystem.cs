namespace FactoryMustScale.Simulation.Core
{
    using FactoryMustScale.Simulation.Item;

    /// <summary>
    /// Authoritative factory tick step order.
    ///
    /// This enum is intentionally explicit because we want one canonical sequence
    /// shared by tests, debugging tools, and future domain systems.
    /// </summary>
    public enum FactoryTickStep : byte
    {
        InputAndEventHandling = 0,
        CellProcessUpdate = 1,
        PublishEventsForNextTick = 2,
    }

    /// <summary>
    /// State for the core factory loop skeleton.
    ///
    /// Notes:
    /// - This structure only captures core-loop flow and deterministic bookkeeping.
    /// - Domain-specific systems (items, energy, logic, combat) should be added later by
    ///   implementing phase bodies, not by changing phase order.
    /// - PhaseTraceBuffer is optional and intended for deterministic tests/instrumentation.
    /// </summary>
    public struct FactoryCoreLoopState
    {
        public int MaxFactoryTicks;
        public int FactoryTicksExecuted;
        public bool Running;

        // Deterministic phase counters for debugging/tests.
        public int InputAndEventHandlingCount;
        public int CellProcessUpdateCount;
        public int PublishEventsForNextTickCount;

        // Optional preallocated trace buffer.
        public int[] PhaseTraceBuffer;
        public int PhaseTraceCount;

        // Item simulation inputs.
        public Layer FactoryLayer;
        public int FactoryPayloadItemChannelIndex;
        public ItemTransportAlgorithm ItemTransportAlgorithm;

        // Item transport scratch buffers (reused, no hot-path allocations).
        public int[] ItemPayloadRead;
        public int[] ItemPayloadWrite;
        public int[] ItemIntentTargetBySource;
        public int[] ItemWinnerSourceByTarget;
        public int[] ItemWinningTargetBySource;
        public byte[] ItemCanExecuteMoveBySource;
        public int[] ItemVisitStampBySource;
    }

    /// <summary>
    /// Core factory loop skeleton with explicit phase ordering.
    ///
    /// Phase contract (per tick):
    /// 1) InputAndEventHandling: read player/system input and consume incoming tick events.
    /// 2) CellProcessUpdate: execute deterministic per-cell processing for this tick.
    /// 3) PublishEventsForNextTick: emit cell process results as events consumed next tick.
    ///
    /// This ordering matches the simulation rules document and should remain stable.
    /// </summary>
    public struct FactoryCoreLoopSystem : ISimulationSystem<FactoryCoreLoopState>
    {
        public void Tick(ref FactoryCoreLoopState state, int tickIndex)
        {
            if (!state.Running)
            {
                return;
            }

            InputAndEventHandling(ref state, tickIndex);
            CellProcessUpdate(ref state, tickIndex);
            PublishEventsForNextTick(ref state, tickIndex);

            state.FactoryTicksExecuted++;
            if (state.FactoryTicksExecuted >= state.MaxFactoryTicks)
            {
                state.Running = false;
            }
        }

        private static void InputAndEventHandling(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.InputAndEventHandlingCount++;
            AppendTrace(ref state, FactoryTickStep.InputAndEventHandling, tickIndex);
        }

        private static void CellProcessUpdate(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.CellProcessUpdateCount++;
            ItemTransportPhaseSystem.Run(ref state);
            AppendTrace(ref state, FactoryTickStep.CellProcessUpdate, tickIndex);
        }

        private static void PublishEventsForNextTick(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.PublishEventsForNextTickCount++;
            AppendTrace(ref state, FactoryTickStep.PublishEventsForNextTick, tickIndex);
        }

        private static void AppendTrace(ref FactoryCoreLoopState state, FactoryTickStep step, int tickIndex)
        {
            if (state.PhaseTraceBuffer == null || state.PhaseTraceCount >= state.PhaseTraceBuffer.Length)
            {
                return;
            }

            // Packed as (tickIndex * 10) + step for deterministic trace assertions.
            state.PhaseTraceBuffer[state.PhaseTraceCount] = (tickIndex * 10) + (int)step;
            state.PhaseTraceCount++;
        }
    }
}
