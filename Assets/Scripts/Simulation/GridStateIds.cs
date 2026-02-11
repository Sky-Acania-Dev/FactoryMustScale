namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Shared state-id table for all grid-backed layers.
    ///
    /// Notes:
    /// - GridCellData.StateId remains an int for hot-path flexibility.
    /// - Terrain and factory entries are grouped into separate numeric ranges for clarity.
    /// - Terrain layer may still directly use TerrainType values where preferred.
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

        // Terrain placeholders (optional; TerrainType can also be used directly)
        TerrainNone = 100,
        TerrainGround = 101,
        TerrainWater = 102,
        TerrainCliff = 103,
        TerrainBlocked = 104,
        TerrainResourceDeposit = 105,
        TerrainGeothermalSite = 106,
    }
}
