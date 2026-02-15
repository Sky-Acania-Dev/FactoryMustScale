namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    /// <summary>
    /// Item-domain simulation entry point for FactoryCoreLoopSystem.RunSimulation.
    /// </summary>
    public static class ItemTransportPhaseSystem
    {
        public static void Run(ref FactoryCoreLoopState state)
        {
            switch (state.ItemTransportAlgorithm)
            {
                case ItemTransportAlgorithm.None:
                    return;
                case ItemTransportAlgorithm.ConveyorArbitratedPropagationV1:
                    ConveyorArbitratedPropagationSystem.Run(ref state);
                    return;
                case ItemTransportAlgorithm.ConveyorMindustryGraphExperimental:
                    ConveyorMindustryGraphSystem.Run(ref state);
                    return;
                default:
                    return;
            }
        }
    }
}
