namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Practical v1 runtime payload for one deterministic simulation cell.
    ///
    /// Use-case guidance:
    /// - StateId: what the cell is (for example conveyor, crafter core, crafter input/output port).
    /// - VariantId: packed variant data (suggested: orientation + subtype + build/destruct stage).
    /// - Flags: fast booleans (powered, connected, can accept payload, can output payload, blocked, etc...).
    /// - LastUpdatedTick: deterministic "last touched" marker for debugging/extraction.
    /// - ChangeCount: monotonically increasing per-cell version for dirty checks.
    ///
    /// Multi-cell factory note:
    /// - A single machine can span multiple cells by assigning different StateId/VariantId/Flags values
    ///   to each occupied tile (for example separate input and output cells for one crafter).
    ///
    /// Payload note:
    /// - Optional per-cell dynamic payload data should live in layer-owned payload channels that share
    ///   the same index scheme, keeping this struct compact and cache-friendly.
    /// </summary>
    public struct GridCellData
    {
        // Authoritative structural state for this cell (empty, conveyor, crafter-part, etc...).
        public int StateId;

        // Optional orientation/variant channel for the same StateId.
        public int VariantId;

        // Bit flags for compact booleans (powered, blocked, reserved, io marker, ...).
        public uint Flags;

        // Deterministic bookkeeping used by systems and debugging.
        public int LastUpdatedTick;
        public int ChangeCount;
    }
}
