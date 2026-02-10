namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Suggested state-id table for simulation cells.
    ///
    /// Notes:
    /// - GridCellData.StateId remains an int for hot-path flexibility.
    /// - These constants are a shared meaning table to avoid magic numbers.
    /// - Values can be extended as additional systems are introduced.
    /// </summary>
    public enum GridStateId
    {
        Empty = 0,

        // Conveyor family
        Conveyor = 1,
        Splitter = 2,
        Merger = 3,

        // Multi-cell crafter roles
        CrafterCore = 10,
        CrafterInputPort = 11,
        CrafterOutputPort = 12,

        // Other foundational building blocks
        Storage = 20,
        PowerPole = 21,
    }
}
