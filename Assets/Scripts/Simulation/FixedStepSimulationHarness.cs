using System;

namespace FactoryMustScale.Simulation
{
    public sealed class FixedStepSimulationHarness<TState, TSystem>
        where TSystem : struct, ISimulationSystem<TState>
    {
        private TState _state;
        private TSystem _system;
        private int _currentTick;

        public FixedStepSimulationHarness(TState initialState, TSystem system)
        {
            _state = initialState;
            _system = system;
            _currentTick = 0;
        }

        public int CurrentTick => _currentTick;

        public TState State => _state;

        public void Tick(int tickCount)
        {
            if (tickCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickCount));
            }

            for (int i = 0; i < tickCount; i++)
            {
                _system.Tick(ref _state, _currentTick);
                _currentTick++;
            }
        }
    }
}
