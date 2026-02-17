namespace FactoryMustScale.Simulation.Core
{
    using FactoryMustScale.Simulation.Item;

    /// <summary>
    /// Authoritative factory tick step order.
    ///
    /// This enum is intentionally explicit because we want one canonical sequence
    /// shared by tests, debugging tools, and future domain systems (item/energy/logic/combat).
    /// </summary>
    public enum FactoryTickStep : byte
    {
        IngestEvents = 0,
        ApplyEvents = 1,
        PrepareSimulation = 2,
        RunSimulation = 3,
        CommitResult = 4,
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
        public int IngestEventsCount;
        public int ApplyEventsCount;
        public int PrepareSimulationCount;
        public int RunSimulationCount;
        public int CommitResultCount;

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
    /// Current scope:
    /// - Establishes and documents the final high-level tick cycle.
    /// - Does not implement domain simulation logic yet.
    ///
    /// Phase contract (per tick):
    /// 1) IngestEvents: pull high-rate input/combat events into the factory tick boundary.
    /// 2) ApplyEvents: apply structural commands (build/remove/rotate/reconfigure).
    /// 3) PrepareSimulation: resolve derived data snapshots for the tick.
    /// 4) RunSimulation: execute deterministic domain simulation updates.
    /// 5) ExtractOutputs: publish results/events/debug output after state update.
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

            IngestEvents(ref state, tickIndex);
            ApplyEvents(ref state, tickIndex);
            PrepareSimulation(ref state, tickIndex);
            RunSimulation(ref state, tickIndex);
            ExtractOutputs(ref state, tickIndex);

            state.FactoryTicksExecuted++;
            if (state.FactoryTicksExecuted >= state.MaxFactoryTicks)
            {
                state.Running = false;
            }
        }

        private static void IngestEvents(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.IngestEventsCount++;
            AppendTrace(ref state, FactoryTickStep.IngestEvents, tickIndex);
        }

        private static void ApplyEvents(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.ApplyEventsCount++;
            AppendTrace(ref state, FactoryTickStep.ApplyEvents, tickIndex);
        }

        private static void PrepareSimulation(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.PrepareSimulationCount++;
            AppendTrace(ref state, FactoryTickStep.PrepareSimulation, tickIndex);
        }

        private static void RunSimulation(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.RunSimulationCount++;
            ItemTransportPhaseSystem.Run(ref state);
            AppendTrace(ref state, FactoryTickStep.RunSimulation, tickIndex);
        }

        private static void ExtractOutputs(ref FactoryCoreLoopState state, int tickIndex)
        {
            state.CommitResultCount++;
            AppendTrace(ref state, FactoryTickStep.CommitResult, tickIndex);
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
