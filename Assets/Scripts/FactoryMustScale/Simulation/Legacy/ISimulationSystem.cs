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
    /// Two-phase contract:
    /// - TickCommit is the only phase allowed to mutate authoritative state.
    /// - TickCompute reads authoritative state and emits next tick events only.
    /// </summary>
    public interface ISimulationSystem<TState>
    {
        void TickCommit(ref TState state, int tickIndex, ref EventBuffer prev);
        void TickCompute(ref TState state, int tickIndex, ref EventBuffer next);
    }
}
