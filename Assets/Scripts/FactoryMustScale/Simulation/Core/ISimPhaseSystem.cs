namespace FactoryMustScale.Simulation.Core
{
    public interface ISimPhaseSystem
    {
        void ExternalIngest(in SimClock clock);

        void Compute(in SimClock clock);

        void Commit(in SimClock clock);
    }
}
