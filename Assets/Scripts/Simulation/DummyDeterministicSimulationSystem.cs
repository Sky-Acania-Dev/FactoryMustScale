namespace FactoryMustScale.Simulation
{
    public struct DummySimulationState
    {
        public int Counter;
        public int Checksum;
    }

    public struct DummyDeterministicSimulationSystem : ISimulationSystem<DummySimulationState>
    {
        private const int CounterStep = 3;
        private const int ChecksumMultiplier = 1664525;
        private const int ChecksumIncrement = 1013904223;

        public void Tick(ref DummySimulationState state, int tickIndex)
        {
            unchecked
            {
                state.Counter += CounterStep;
                state.Checksum = (state.Checksum * ChecksumMultiplier) + ChecksumIncrement + state.Counter + tickIndex;
            }
        }
    }
}
