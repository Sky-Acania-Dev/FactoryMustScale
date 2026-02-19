using FactoryMustScale.Simulation.Core;
using UnityEngine;

namespace FactoryMustScale.Runtime
{
    /// <summary>
    /// Unity-facing wrapper that advances the deterministic simulation loop at 32 ticks per second.
    /// </summary>
    public sealed class SimulationLoopDriver : MonoBehaviour
    {
        private const float UnitTickDeltaSeconds = 1.0f / 32.0f;

        private readonly ISimPhaseSystem[] _systems = System.Array.Empty<ISimPhaseSystem>();
        private SimLoop _simLoop;
        private float _accumulatorSeconds;

        private void Awake()
        {
            _simLoop = new SimLoop(_systems);
            _accumulatorSeconds = 0.0f;
        }

        private void Update()
        {
            _accumulatorSeconds += Time.deltaTime;

            while (_accumulatorSeconds >= UnitTickDeltaSeconds)
            {
                _simLoop.Tick();
                _accumulatorSeconds -= UnitTickDeltaSeconds;
            }
        }
    }
}
