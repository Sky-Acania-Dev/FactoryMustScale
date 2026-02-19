using System;

namespace FactoryMustScale.Simulation
{
    public enum SimEventEndpointKind : byte
    {
        None = 0,
        Cell = 1,
        Storage = 2,
        World = 3,
    }

    public struct SimEvent
    {
        public SimEventId Id;
        public int Tick;
        public int SourceIndex;
        public int TargetIndex;
        public SimEventEndpointKind SourceKind;
        public SimEventEndpointKind TargetKind;
        public int ItemType;
        public int ItemCount;
        public int ValueA;
        public int ValueB;

        public override string ToString()
        {
            return string.Format(
                "Tick={0} Id={1} Src({2}:{3}) Dst({4}:{5}) Item={6}x{7} A={8} B={9}",
                Tick,
                Id,
                SourceKind,
                SourceIndex,
                TargetKind,
                TargetIndex,
                ItemType,
                ItemCount,
                ValueA,
                ValueB);
        }
    }
}
