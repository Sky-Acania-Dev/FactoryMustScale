namespace FactoryMustScale.Simulation.Domains.Factory.Systems.Transport
{
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Item;
    using FactoryMustScale.Simulation.Legacy;

    /// <summary>
    /// Thin adapter that executes the existing item transport phase system inside the SimLoop 3-phase contract.
    ///
    /// Legacy mapping under canonical 3-phase loop:
    /// - ExternalIngest => SimEvent buffer ingest (BeginTick + PromoteQueuedEvents), no authoritative writes.
    /// - Compute => ItemTransportPhaseSystem.Run + PublishEvents to produce deterministic intents/queued events.
    /// - Commit => ItemTransportPhaseSystem.IngestEvents applies queued transport events to authoritative state.
    /// </summary>
    public sealed class FactoryTransportLegacyAdapter : ISimSystem
    {
        private FactoryCoreLoopState _state;

        public FactoryTransportLegacyAdapter(in FactoryCoreLoopState initialState)
        {
            _state = initialState;
        }

        public int ExternalIngestRunCount { get; private set; }

        public int ComputeRunCount { get; private set; }

        public int CommitRunCount { get; private set; }

        public void ExternalIngest(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            _state.SimEvents.EnsureCapacity(_state.SimEventCapacity > 0 ? _state.SimEventCapacity : 128);
            _state.SimEvents.BeginTick();
            _state.SimEvents.PromoteQueuedEvents();
            ExternalIngestRunCount++;
        }

        public void Compute(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            ItemTransportPhaseSystem.Run(ref _state);
            ItemTransportPhaseSystem.PublishEvents(ref _state);
            ComputeRunCount++;
        }

        public void Commit(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            ItemTransportPhaseSystem.IngestEvents(ref _state);
            CommitRunCount++;
        }
    }
}
