namespace FactoryMustScale.Simulation.Item
{
    /// <summary>
    /// Selects which deterministic item transport implementation runs during the core simulation phase.
    /// </summary>
    public enum ItemTransportAlgorithm : byte
    {
        None = 0,
        ConveyorArbitratedPropagationV1 = 1,
        ConveyorMindustryGraphExperimental = 2,
    }
}
