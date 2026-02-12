using System;

namespace FactoryMustScale.Simulation
{
    [Flags]
    public enum CardinalDirectionMask : byte
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3,
        All = Up | Right | Down | Left,
    }

    public enum FactoryProcessType : byte
    {
        None = 0,
        PassThrough = 1,
        Extract = 2,
        Craft = 3,
        GeneratePower = 4,
        ConsumePower = 5,
        ConvertPower = 6,
    }

    public enum ProcessResourceType : byte
    {
        None = 0,
        ElectricPower = 1,
        Heat = 2,
        MechanicalWork = 3,
        Reserved = 255,
    }

    [Serializable]
    public struct FactoryProcessSlotData
    {
        public CardinalDirectionMask Directions;
        public int ItemIdFilter;
        public int MaxItemsPerTick;
    }

    [Serializable]
    public struct ProcessItemStackData
    {
        public int ItemId;
        public int Amount;
    }

    [Serializable]
    public struct ProcessResourceDeltaData
    {
        public ProcessResourceType ResourceType;
        // Positive values produce resource, negative values consume resource.
        public int Amount;
    }

    [Serializable]
    public struct FactoryProcessRecipeData
    {
        public const int ItemInputCount = 3;
        public const int ItemOutputCount = 2;
        public const int ResourceDeltaCount = 4;

        // Fixed-size recipe item and resource dimensions for deterministic data layout.
        public ProcessItemStackData[] ItemInputs;
        public ProcessItemStackData[] ItemOutputs;
        public ProcessResourceDeltaData[] ResourceDeltas;
    }

    /// <summary>
    /// Baked runtime process definition for one factory state id.
    /// Dynamic counters (for example process progress) must live in layer payload channels.
    /// </summary>
    [Serializable]
    public struct FactoryCellProcessData
    {
        public int StateId;
        public FactoryProcessType ProcessType;

        // Number of simulation ticks required for one process cycle.
        // Use value 1 for one-tick transfer cells such as basic conveyors.
        public int ProcessDurationTicks;

        // Factory-layer payload channel index where per-cell process progress is stored.
        // A negative value means this state does not use progress tracking.
        public int ProcessProgressPayloadChannelIndex;

        public FactoryProcessSlotData[] InputSlots;
        public FactoryProcessSlotData[] OutputSlots;
        public FactoryProcessRecipeData[] Recipes;
    }
}
