namespace FactoryMustScale.Simulation.ItemTransport
{
    using FactoryMustScale.Simulation.Legacy;

    /// <summary>
    /// Reusable transport buffers for deterministic belt movement.
    /// Buffers are resized only when grid cell count changes.
    /// </summary>
    public static class BeltTransportBuffers
    {
        public const int Invalid = -1;
        public const int Empty = 0;

        public static void EnsureBuffers(ref FactoryCoreLoopState state, int cellCount)
        {
            if (state.ItemIntentTargetBySource == null || state.ItemIntentTargetBySource.Length != cellCount)
            {
                state.ItemIntentTargetBySource = new int[cellCount];
            }

            if (state.ItemResolvedSourceByTarget == null || state.ItemResolvedSourceByTarget.Length != cellCount)
            {
                state.ItemResolvedSourceByTarget = new int[cellCount];
            }

            if (state.ItemResolvedTargetBySource == null || state.ItemResolvedTargetBySource.Length != cellCount)
            {
                state.ItemResolvedTargetBySource = new int[cellCount];
            }

            if (state.ItemNextPayloadByCell == null || state.ItemNextPayloadByCell.Length != cellCount)
            {
                state.ItemNextPayloadByCell = new int[cellCount];
            }

            if (state.ItemNextTransportProgressByCell == null || state.ItemNextTransportProgressByCell.Length != cellCount)
            {
                state.ItemNextTransportProgressByCell = new int[cellCount];
            }

            if (state.ItemPayloadByCell == null || state.ItemPayloadByCell.Length != cellCount)
            {
                state.ItemPayloadByCell = new int[cellCount];
            }

            if (state.ItemTransportProgressByCell == null || state.ItemTransportProgressByCell.Length != cellCount)
            {
                state.ItemTransportProgressByCell = new int[cellCount];
            }
        }
    }
}
