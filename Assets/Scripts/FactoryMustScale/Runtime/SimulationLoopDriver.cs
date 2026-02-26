using FactoryMustScale.Simulation;
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

        private ISimSystem[] _systems = System.Array.Empty<ISimSystem>();
        private SimLoop _simLoop;
        private float _accumulatorSeconds;
        private int _unitTick;
        private FactoryTransportLegacyAdapter _factoryTransportAdapter;
        private Layer _factoryLayer;

        public int UnitTick => _unitTick;

        public int MinimapGridWidth => _factoryLayer != null ? _factoryLayer.Width : 0;

        public int MinimapGridHeight => _factoryLayer != null ? _factoryLayer.Height : 0;

        public GridCellData[] MinimapCells => _factoryLayer != null ? _factoryLayer.CellData : null;

        public int[] MinimapItemIdByCell => _factoryTransportAdapter != null ? _factoryTransportAdapter.ItemIdByCell : null;

        public void ConfigureFactoryTransportState(in CoreLoopState state)
        {
            _factoryLayer = state.FactoryLayer;
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
                CoreLoopState initialFactoryState = new CoreLoopState
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
