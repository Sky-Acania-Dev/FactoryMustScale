namespace FactoryMustScale.Simulation
{
    public interface ISimulationSystem<TState>
    {
        void Tick(ref TState state, int tickIndex);
    }
}
