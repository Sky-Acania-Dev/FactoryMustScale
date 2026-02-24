namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Canonical deterministic simulation system contract.
    ///
    /// Phase contract:
    /// 1) PreCompute:
    ///    A) ingest external commands/intents into transient buffers,
    ///    B) apply structural cell edits early (build/remove/rotate) when required.
    /// 2) Compute: read-only computation that emits intents/deltas/events.
    /// 3) Commit: apply non-structural authoritative mutations only.
    /// </summary>
    public interface ISimSystem
    {
        void PreCompute(ref SimContext ctx);

        void Compute(ref SimContext ctx);

        void Commit(ref SimContext ctx);
    }
}
