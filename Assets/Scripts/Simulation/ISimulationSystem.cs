namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Minimal simulation contract for fixed-step systems.
    ///
    /// Rationale (AGENTS.md + Docs/SimRules.md):
    /// - Exists to isolate pure simulation from Unity-facing adapters.
    /// - Accepts state by <c>ref</c> and an explicit tick index to keep update order explicit and deterministic.
    /// - Avoids forbidden constructs by design: no UnityEngine dependency, no LINQ, no collection iteration policy hidden in the API.
    ///
    /// Assumption:
    /// - Implementers are pure and deterministic for equal initial state + equal tick sequence.
    ///
    /// Expansion path:
    /// - Keep this contract stable and add optional input/output buffers as additional parameters when input ingestion
    ///   and output extraction phases are introduced (without changing fixed-step semantics).
    /// </summary>
    public interface ISimulationSystem<TState>
    {
        void Tick(ref TState state, int tickIndex);
    }
}
