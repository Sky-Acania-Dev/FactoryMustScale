namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Tiny non-gameplay system for verifying 3-phase execution order.
    /// </summary>
    public sealed class SimLoopSmokeTestSystem : ISimSystem
    {
        public int LastPhaseMarker { get; private set; }
        public int LastObservedTick { get; private set; }

        public void ExternalIngest(ref SimContext ctx)
        {
            LastObservedTick = ctx.Clock.UnitTick;
            LastPhaseMarker = 1;
        }

        public void Compute(ref SimContext ctx)
        {
            if (LastPhaseMarker == 1)
            {
                LastPhaseMarker = 2;
            }
        }

        public void Commit(ref SimContext ctx)
        {
            if (LastPhaseMarker == 2)
            {
                LastPhaseMarker = 3;
            }
        }
    }
}
