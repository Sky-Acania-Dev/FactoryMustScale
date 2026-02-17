namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    /// <summary>
    /// Item-domain simulation entry points mapped to core loop phases.
    /// </summary>
    public static class ItemTransportPhaseSystem
    {
        public static void IngestEvents(ref FactoryCoreLoopState state)
        {
            switch (state.ItemTransportAlgorithm)
            {
                case ItemTransportAlgorithm.None:
                    return;
                case ItemTransportAlgorithm.ConveyorArbitratedPropagationV1:
                    ConveyorArbitratedPropagationSystem.IngestEvents(ref state);
                    return;
                case ItemTransportAlgorithm.ConveyorGraphBasedExperimental:
                    return;
                default:
                    return;
            }
        }

        public static void Run(ref FactoryCoreLoopState state)
        {
            switch (state.ItemTransportAlgorithm)
            {
                case ItemTransportAlgorithm.None:
                    return;
                case ItemTransportAlgorithm.ConveyorArbitratedPropagationV1:
                    ConveyorArbitratedPropagationSystem.ProcessCells(ref state);
                    return;
                case ItemTransportAlgorithm.ConveyorGraphBasedExperimental:
                    return;
                default:
                    return;
            }
        }

        public static void PublishEvents(ref FactoryCoreLoopState state)
        {
            switch (state.ItemTransportAlgorithm)
            {
                case ItemTransportAlgorithm.None:
                    return;
                case ItemTransportAlgorithm.ConveyorArbitratedPropagationV1:
                    ConveyorArbitratedPropagationSystem.PublishEvents(ref state);
                    return;
                case ItemTransportAlgorithm.ConveyorGraphBasedExperimental:
                    return;
                default:
                    return;
            }
        }
    }
}
