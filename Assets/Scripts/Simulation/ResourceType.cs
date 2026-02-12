namespace FactoryMustScale.Simulation
{
    public enum ResourceType : byte
    {
        None = 0,
        Ore = 1,
        Liquid = 2,
        Geothermal = 3,
        OreTier2 = 4,
        OreTier3 = 5
    }

    [System.Flags]
    public enum ResourceTypeMask : uint
    {
        None = 0u,
        NoneResource = 1u << 0,
        Ore = 1u << 1,
        Liquid = 1u << 2,
        Geothermal = 1u << 3,
        OreTier2 = 1u << 4,
        OreTier3 = 1u << 5,
        Any = NoneResource | Ore | Liquid | Geothermal | OreTier2 | OreTier3
    }
}
