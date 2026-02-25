namespace FactoryMustScale.Simulation.Domains.Factory.Systems.Transport
{
    using FactoryMustScale.Simulation;
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Item;
    using FactoryMustScale.Simulation.Legacy;

    /// <summary>
    /// Thin adapter that executes the existing item transport phase system inside the SimLoop 3-phase contract.
    ///
    /// Legacy mapping under canonical 3-phase loop:
    /// - PreCompute => SimEvent buffer ingest (BeginTick + PromoteQueuedEvents), no authoritative writes.
    /// - Compute => ItemTransportPhaseSystem.Run + PublishEvents to produce deterministic intents/queued events.
    /// - Commit => ItemTransportPhaseSystem.IngestEvents applies queued transport events to authoritative state.
    /// </summary>
    public sealed class FactoryTransportLegacyAdapter : ISimSystem, ISimHashSource
    {
        private FactoryCoreLoopState _state;

        public FactoryTransportLegacyAdapter(in FactoryCoreLoopState initialState)
        {
            _state = initialState;
        }

        public int PreComputeRunCount { get; private set; }

        public int ComputeRunCount { get; private set; }

        public int CommitRunCount { get; private set; }

        public int Width => _state.FactoryLayer != null ? _state.FactoryLayer.Width : 0;

        public int Height => _state.FactoryLayer != null ? _state.FactoryLayer.Height : 0;

        public GridCellData[] Cells => _state.FactoryLayer != null ? _state.FactoryLayer.CellData : null;

        public int[] ItemIdByCell => _state.ItemPayloadByCell;

        public void PreCompute(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            _state.SimEvents.EnsureCapacity(_state.SimEventCapacity > 0 ? _state.SimEventCapacity : 128);
            _state.SimEvents.BeginTick();
            _state.SimEvents.PromoteQueuedEvents();
            PreComputeRunCount++;
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

        public void AppendHash(ref SimHashBuilder builder)
        {
            if (_state.FactoryLayer != null)
            {
                _state.FactoryLayer.AppendDeterministicHash(ref builder);
            }

            builder.AppendInt(_state.FactoryTicksExecuted);
            AppendArray(ref builder, _state.StorageItemCountByCell, _state.StorageItemCountByCell != null ? _state.StorageItemCountByCell.Length : 0);
            AppendArray(ref builder, _state.ItemPayloadByCell, _state.ItemPayloadByCell != null ? _state.ItemPayloadByCell.Length : 0);
            AppendArray(ref builder, _state.ItemTransportProgressByCell, _state.ItemTransportProgressByCell != null ? _state.ItemTransportProgressByCell.Length : 0);
            AppendArray(ref builder, _state.ItemMergerRoundRobinCursorByCell, _state.ItemMergerRoundRobinCursorByCell != null ? _state.ItemMergerRoundRobinCursorByCell.Length : 0);
        }

        private static void AppendArray(ref SimHashBuilder builder, int[] values, int activeCount)
        {
            if (values == null || activeCount <= 0)
            {
                builder.AppendInt(0);
                return;
            }

            if (activeCount > values.Length)
            {
                activeCount = values.Length;
            }

            builder.AppendInt(activeCount);
            for (int i = 0; i < activeCount; i++)
            {
                builder.AppendInt(values[i]);
            }
        }
    }
}
