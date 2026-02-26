using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Transport;
using FactoryMustScale.Simulation.Item;
using FactoryMustScale.Simulation.Legacy;
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

        private ISimSystem[] _systems = System.Array.Empty<ISimSystem>();
        private SimLoop _simLoop;
        private float _accumulatorSeconds;
        private int _unitTick;
        private FactoryTransportLegacyAdapter _factoryTransportAdapter;

        public int UnitTick => _unitTick;

        public int MinimapGridWidth => _factoryTransportAdapter != null ? _factoryTransportAdapter.Width : 0;

        public int MinimapGridHeight => _factoryTransportAdapter != null ? _factoryTransportAdapter.Height : 0;

        public GridCellData[] MinimapCells => _factoryTransportAdapter != null ? _factoryTransportAdapter.Cells : null;

        public int[] MinimapItemIdByCell => _factoryTransportAdapter != null ? _factoryTransportAdapter.ItemIdByCell : null;

        public void ConfigureFactoryTransportState(in FactoryCoreLoopState state)
        {
            _factoryTransportAdapter = new FactoryTransportLegacyAdapter(in state);
            _systems = new ISimSystem[]
            {
                _factoryTransportAdapter,
            };

            // Rebuild to apply runtime reconfiguration.
            if (_simLoop != null)
            {
                _simLoop = new SimLoop(_systems);
            }
        }

        public void TickOnce()
        {
            if (_simLoop == null)
            {
                return;
            }

            _unitTick++;
            SimClock clock = new SimClock(_unitTick);
            _simLoop.Tick(in clock);
            Debug.Log("Tick: " + _unitTick);
        }

        private void Awake()
        {
            if (_simLoop == null)
            {
                SetLoop();
            }
        }

        internal void ResetUnitTick()
        {
            _accumulatorSeconds = 0.0f;
            _unitTick = 0;
        }

        public void SetLoop()
        {
            if (_systems.Length == 0 && _useFactoryTransportLegacyAdapter)
            {
                FactoryCoreLoopState initialFactoryState = new FactoryCoreLoopState
                {
                    ItemTransportAlgorithm = ItemTransportAlgorithm.SimplePush,
                };

                _factoryTransportAdapter = new FactoryTransportLegacyAdapter(in initialFactoryState);
                _systems = new ISimSystem[]
                {
                    _factoryTransportAdapter,
                };
            }

            _simLoop = new SimLoop(_systems);
            _accumulatorSeconds = 0.0f;
            _unitTick = 0;
        }

        private void FixedUpdate()
        {
            _accumulatorSeconds += Time.fixedDeltaTime;

            while (_accumulatorSeconds >= UnitTickDeltaSeconds)
            {
                TickOnce();
                _accumulatorSeconds -= UnitTickDeltaSeconds;
            }
        }
    }
}
