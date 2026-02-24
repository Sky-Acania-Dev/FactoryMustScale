namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Authoritative simulation clock based on unit ticks.
    /// UnitTick advances at 32 Hz.
    /// FactoryTick occurs every 4 UnitTicks.
    /// EnvTick occurs every 32 UnitTicks.
    /// </summary>
    public readonly struct SimClock
    {
        public const int UnitTicksPerFactoryTick = 4;
        public const int UnitTicksPerEnvTick = 32;

        public SimClock(int unitTick)
        {
            UnitTick = unitTick;
        }

        public int UnitTick { get; }

        public int FactoryTick => UnitTick / UnitTicksPerFactoryTick;

        public int EnvTick => UnitTick / UnitTicksPerEnvTick;

        public bool IsFactoryTick => IsTickFactoryTick(UnitTick);

        public bool IsEnvTick => IsTickEnvTick(UnitTick);

        public static bool IsTickFactoryTick(int unitTick)
        {
            return (unitTick % UnitTicksPerFactoryTick) == 0;
        }

        public static bool IsTickEnvTick(int unitTick)
        {
            return (unitTick % UnitTicksPerEnvTick) == 0;
        }
    }
}
