namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Minimal runtime state used by the dummy system.
    ///
    /// Why this struct exists:
    /// - Demonstrates the project rule that runtime simulation state should be plain data (no Unity references).
    /// - Keeps the deterministic baseline easy to inspect and serialize later.
    /// </summary>
    public struct DummySimulationState
    {
        public int Counter;
        public int Checksum;
    }

    /// <summary>
    /// A tiny deterministic system used as a harness baseline.
    ///
    /// Why this class exists:
    /// - Provides a concrete non-Unity simulation workload for EditMode verification.
    /// - Makes deterministic behavior measurable with exact expected values.
    ///
    /// Fixed-step behavior:
    /// - Performs exactly one deterministic state transition for each harness tick.
    /// - Depends only on current state and explicit tick index input.
    ///
    /// Determinism strategy:
    /// - Uses only integer math and constants.
    /// - Uses <c>unchecked</c> arithmetic so overflow behavior is explicit and repeatable.
    ///
    /// Forbidden-construct avoidance:
    /// - No allocations in <c>Tick</c>.
    /// - No LINQ, no <c>foreach</c>, no UnityEngine API calls, no GetComponent.
    ///
    /// Assumptions:
    /// - Integer overflow semantics remain the same in target runtime (standard C# unchecked behavior).
    ///
    /// Possible improvement relative to rules:
    /// - Replace this toy transition with real system state arrays once factory-layer simulation begins,
    ///   while preserving deterministic indexed iteration and allocation-free hot path.
    /// </summary>
    public struct DummyDeterministicSimulationSystem : ISimulationSystem<DummySimulationState>
    {
        private const int CounterStep = 3;
        private const int ChecksumMultiplier = 1664525;
        private const int ChecksumIncrement = 1013904223;

        public void Tick(ref DummySimulationState state, int tickIndex)
        {
            unchecked
            {
                state.Counter += CounterStep;
                state.Checksum = (state.Checksum * ChecksumMultiplier) + ChecksumIncrement + state.Counter + tickIndex;
            }
        }
    }
}
