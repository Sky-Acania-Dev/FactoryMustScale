namespace FactoryMustScale.Simulation.Domains.Factory.Systems.Transport
{
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Item;

    /// <summary>
    /// Thin adapter that executes the existing item transport phase system inside the SimLoop 3-phase contract.
    ///
    /// Legacy mapping (behavior-preserving):
    /// - ExternalIngest => ItemTransportPhaseSystem.IngestEvents (apply previous scheduled transfers/events)
    /// - Compute => ItemTransportPhaseSystem.Run (compute/process transport intents and arbitration)
    /// - Commit => ItemTransportPhaseSystem.PublishEvents (schedule/publish next-tick transfers/events)
    /// </summary>
    public sealed class FactoryTransportLegacyAdapter : ISimPhaseSystem
    {
        private FactoryCoreLoopState _state;

        public FactoryTransportLegacyAdapter(in FactoryCoreLoopState initialState)
        {
            _state = initialState;
        }

        public int ExternalIngestRunCount { get; private set; }

        public int ComputeRunCount { get; private set; }

        public int CommitRunCount { get; private set; }

        public void ExternalIngest(in SimClock clock)
        {
            if (!SimClock.IsFactoryTick(clock.UnitTick))
            {
                return;
            }

            ItemTransportPhaseSystem.IngestEvents(ref _state);
            ExternalIngestRunCount++;
        }

        public void Compute(in SimClock clock)
        {
            if (!SimClock.IsFactoryTick(clock.UnitTick))
            {
                return;
            }

            ItemTransportPhaseSystem.Run(ref _state);
            ComputeRunCount++;
        }

        public void Commit(in SimClock clock)
        {
            if (!SimClock.IsFactoryTick(clock.UnitTick))
            {
                return;
            }

            ItemTransportPhaseSystem.PublishEvents(ref _state);
            CommitRunCount++;
        }
    }
}
