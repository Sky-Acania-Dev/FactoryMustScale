namespace FactoryMustScale.Simulation
{
    public enum TerrainType : byte
    {
        None = 0,
        Ground = 1,
        Water = 2,
        Cliff = 3,
        Blocked = 4,
        ResourceDeposit = 5,
        GeothermalSite = 6
    }

    [System.Flags]
    public enum TerrainTypeMask : uint
    {
        None = 0u,
        NoneTerrain = 1u << 0,
        Ground = 1u << 1,
        Water = 1u << 2,
        Cliff = 1u << 3,
        Blocked = 1u << 4,
        ResourceDeposit = 1u << 5,
        GeothermalSite = 1u << 6,
        Any = NoneTerrain | Ground | Water | Cliff | Blocked | ResourceDeposit | GeothermalSite
    }
}
