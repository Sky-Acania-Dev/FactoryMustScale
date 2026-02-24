namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Deterministic runtime state for factory structural cell modifications.
    /// </summary>
    public struct FactoryBuildSystemState
    {
        public Layer TerrainLayer;
        public Layer FactoryLayer;
        public int TerrainResourceChannelIndex;

        public BuildableRuleData[] BuildableRules;
        public FactoryFootprintData[] Footprints;

        // External command input and deterministic per-tick output buffers.
        public FactoryCommandQueue CommandQueue;
        public FactoryCommandQueue StructuralIntentBuffer;
        public FactoryCommandResultBuffer CommandResults;
        public bool StopProcessingOnFailure;

        // Conflict-resolution scratch (reused).
        public int[] StructuralIntentWinnerByCell;
        public int[] StructuralIntentWriteStampByCell;

        // Deterministic active sets refreshed after structural mutations.
        public int[] ActiveBeltCellIndices;
        public int ActiveBeltCellCount;
        public int[] ActiveProcessCellIndices;
        public int ActiveProcessCellCount;
    }
}
