namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Canonical deterministic simulation system contract.
    ///
    /// Phase contract:
    /// 1) ExternalIngest: read external commands/intents into transient buffers.
    /// 2) Compute: read-only computation that emits intents/deltas/events.
    /// 3) Commit: the only phase allowed to mutate authoritative state.
    /// </summary>
    public interface ISimSystem
    {
        void ExternalIngest(ref SimContext ctx);

        void Compute(ref SimContext ctx);

        void Commit(ref SimContext ctx);
    }
}
