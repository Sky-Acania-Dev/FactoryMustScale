namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Deterministic state hash helper (stub).
    /// </summary>
    public static class SimHash
    {
        public static int Fold(int seed, int value)
        {
            unchecked
            {
                return (seed * 397) ^ value;
            }
        }
    }
}
