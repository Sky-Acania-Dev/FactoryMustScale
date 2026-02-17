namespace FactoryMustScale.Simulation
{
    public enum SimEventId : byte
    {
        None = 0,
        CellCreated = 1,
        CellRemoved = 2,
        CellRotated = 3,
        ItemGenerated = 4,
        ItemTransported = 5,
        ItemMutated = 6,
        ItemStored = 7,
        ItemDeleted = 8,
    }
}
