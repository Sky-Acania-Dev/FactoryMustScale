namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Practical v1 runtime payload for one deterministic simulation cell.
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
