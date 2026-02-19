namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Authoritative simulation clock based on unit ticks.
    /// Unit tick base rate is 32 Hz.
    /// </summary>
    public readonly struct SimClock
    {
        public SimClock(int unitTick)
        {
            UnitTick = unitTick;
        }

        public int UnitTick { get; }

        public static bool IsFactoryTick(int unitTick)
        {
            return (unitTick % 4) == 0;
        }

        public static bool IsEnvTick(int unitTick)
        {
            return (unitTick % 32) == 0;
        }
    }
}
