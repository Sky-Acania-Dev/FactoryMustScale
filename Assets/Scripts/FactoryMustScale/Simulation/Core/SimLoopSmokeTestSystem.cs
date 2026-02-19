namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Tiny non-gameplay system for verifying 3-phase execution order.
    /// </summary>
    public sealed class SimLoopSmokeTestSystem : ISimPhaseSystem
    {
        public int LastPhaseMarker { get; private set; }
        public int LastObservedTick { get; private set; }

        public void ExternalIngest(in SimClock clock)
        {
            LastObservedTick = clock.UnitTick;
            LastPhaseMarker = 1;
        }

        public void Compute(in SimClock clock)
        {
            if (LastPhaseMarker == 1)
            {
                LastPhaseMarker = 2;
            }
        }

        public void Commit(in SimClock clock)
        {
            if (LastPhaseMarker == 2)
            {
                LastPhaseMarker = 3;
            }
        }
    }
}
