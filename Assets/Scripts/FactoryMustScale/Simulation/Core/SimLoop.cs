using System;

namespace FactoryMustScale.Simulation.Core
{
    public sealed class SimLoop
    {
        private readonly ISimPhaseSystem[] _systems;
        private int _unitTick;

        public SimLoop(ISimPhaseSystem[] systems)
        {
            if (systems == null)
            {
                throw new ArgumentNullException(nameof(systems));
            }

            _systems = systems;
            _unitTick = 0;
        }

        public int UnitTick => _unitTick;

        public void Tick()
        {
            _unitTick++;
            SimClock clock = new SimClock(_unitTick);

            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].ExternalIngest(in clock);
            }

            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Compute(in clock);
            }

            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Commit(in clock);
            }
        }
    }
}
