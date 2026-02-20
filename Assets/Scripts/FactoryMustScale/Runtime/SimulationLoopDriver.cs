using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Transport;
using FactoryMustScale.Simulation.Item;
using UnityEngine;

namespace FactoryMustScale.Runtime
{
    /// <summary>
    /// Unity-facing wrapper that advances the deterministic simulation loop at 32 ticks per second.
    /// </summary>
    public sealed class SimulationLoopDriver : MonoBehaviour
    {
        private const float UnitTickDeltaSeconds = 1.0f / 32.0f;

        [SerializeField]
        private bool _useFactoryTransportLegacyAdapter;

        private ISimPhaseSystem[] _systems = System.Array.Empty<ISimPhaseSystem>();
        private SimLoop _simLoop;
        private float _accumulatorSeconds;

        public void ConfigureFactoryTransportState(in FactoryCoreLoopState state)
        {
            _systems = new ISimPhaseSystem[]
            {
                new FactoryTransportLegacyAdapter(in state),
            };
        }

        private void Awake()
        {
            if (_systems.Length == 0 && _useFactoryTransportLegacyAdapter)
            {
                FactoryCoreLoopState initialFactoryState = new FactoryCoreLoopState
                {
                    ItemTransportAlgorithm = ItemTransportAlgorithm.SimplePush,
                };

                _systems = new ISimPhaseSystem[]
                {
                    new FactoryTransportLegacyAdapter(in initialFactoryState),
                };
            }

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
