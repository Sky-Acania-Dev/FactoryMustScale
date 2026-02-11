namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Factory/building state-id table for cells in the factory layer.
    ///
    /// Notes:
    /// - GridCellData.StateId remains an int for hot-path flexibility.
    /// - Terrain layer states are represented by TerrainType, not GridStateId.
    /// </summary>
    public enum GridStateId
    {
        Empty = 0,

        // Factory: transport
        Conveyor = 1,
        Splitter = 2,
        Merger = 3,

        // Factory: extraction and processing
        Miner = 10,
        CrafterCore = 11,
        CrafterInputPort = 12,
        CrafterOutputPort = 13,

        // Factory: storage and power
        Storage = 20,
        PowerGenerator = 21,
        PowerPole = 22,
        PowerPylon = 23,
    }
}
