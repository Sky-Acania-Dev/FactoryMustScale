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

        // Factory: net producers
        Miner = 11,


        // Factory: crafters
        CrafterCore = 21,
        CrafterInputPort = 22,
        CrafterOutputPort = 23,

        // Factory: storage and item sink
        Storage = 31,


        // Power: generation and distribution
        PowerGenerator = 101,
        PowerPole = 102,
        PowerPylon = 103,
    }
}
