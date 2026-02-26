using FactoryMustScale.Runtime.Visualization;
using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization.Toy
{
    /// <summary>
    /// TOY ONLY: deterministic fake belt source for visualization experiments.
    /// Not authoritative simulation and must not be used to validate gameplay systems.
    /// </summary>
    public sealed class ToyBeltScenarioMinimapSource : GridMinimapSourceBase
    {
        private const int ProducedItemId = 1;

        [SerializeField]
        private int _width = 32;

        [SerializeField]
        private int _height = 16;

        [SerializeField]
        private int _minerPeriodTicks = 6;

        [SerializeField]
        private float _tickIntervalSeconds = 0.25f;

        private GridCellData[] _cells;
        private int[] _itemIdByCell;
        private int[] _pathCellIndices;
        private int _minerOutputIndex;
        private int _storageInputIndex;
        private int _currentTick;
        private float _accumulator;

        public override int Width => _width;
        public override int Height => _height;
        public override GridCellData[] Cells => _cells;
        public override int[] ItemIdByCell => _itemIdByCell;
        public override int CurrentTick => _currentTick;

        private void Awake()
        {
            BuildScenario();
        }

        private void FixedUpdate()
        {
            if (_cells == null || _itemIdByCell == null)
            {
                return;
            }

            _accumulator += Time.fixedDeltaTime;
            while (_accumulator >= _tickIntervalSeconds)
            {
                _accumulator -= _tickIntervalSeconds;
                StepOnce();
            }
        }

        private void BuildScenario()
        {
            int clampedWidth = _width < 1 ? 1 : _width;
            int clampedHeight = _height < 1 ? 1 : _height;
            _width = clampedWidth;
            _height = clampedHeight;

            int cellCount = _width * _height;
            _cells = new GridCellData[cellCount];
            _itemIdByCell = new int[cellCount];
            _pathCellIndices = BuildSPath(_width);
            _minerOutputIndex = _pathCellIndices[0];
            _storageInputIndex = _pathCellIndices[_pathCellIndices.Length - 1];
            _currentTick = 0;
            _accumulator = 0.0f;

            for (int i = 0; i < _pathCellIndices.Length; i++)
            {
                int cellIndex = _pathCellIndices[i];
                GridCellData cell = _cells[cellIndex];
                cell.StateId = (int)GridStateId.Conveyor;
                _cells[cellIndex] = cell;
            }

            GridCellData minerCell = _cells[_minerOutputIndex];
            minerCell.StateId = (int)GridStateId.Miner;
            _cells[_minerOutputIndex] = minerCell;

            GridCellData storageCell = _cells[_storageInputIndex];
            storageCell.StateId = (int)GridStateId.Storage;
            _cells[_storageInputIndex] = storageCell;
        }

        private void StepOnce()
        {
            _currentTick++;

            for (int i = _pathCellIndices.Length - 2; i >= 0; i--)
            {
                int fromIndex = _pathCellIndices[i];
                int toIndex = _pathCellIndices[i + 1];

                if (toIndex == _storageInputIndex && _itemIdByCell[toIndex] != 0)
                {
                    _itemIdByCell[toIndex] = 0;
                }

                int payload = _itemIdByCell[fromIndex];
                if (payload == 0 || _itemIdByCell[toIndex] != 0)
                {
                    continue;
                }

                _itemIdByCell[toIndex] = payload;
                _itemIdByCell[fromIndex] = 0;
            }

            int clampedMinerPeriod = _minerPeriodTicks < 1 ? 1 : _minerPeriodTicks;
            if ((_currentTick % clampedMinerPeriod) == 0 && _itemIdByCell[_minerOutputIndex] == 0)
            {
                _itemIdByCell[_minerOutputIndex] = ProducedItemId;
            }
            Debug.Log("Toy Belt Sim Tick: " + _currentTick);
        }

        private static int[] BuildSPath(int width)
        {
            const int left = 2;
            const int right = 28;
            const int topY = 12;
            const int midY = 8;
            const int bottomY = 4;

            int length = (right - left + 1) + (topY - midY) + (right - left) + (midY - bottomY) + (right - left);
            int[] path = new int[length];
            int writeIndex = 0;

            for (int x = left; x <= right; x++)
            {
                path[writeIndex++] = x + topY * width;
            }

            for (int y = topY - 1; y >= midY; y--)
            {
                path[writeIndex++] = right + y * width;
            }

            for (int x = right - 1; x >= left; x--)
            {
                path[writeIndex++] = x + midY * width;
            }

            for (int y = midY - 1; y >= bottomY; y--)
            {
                path[writeIndex++] = left + y * width;
            }

            for (int x = left + 1; x <= right; x++)
            {
                path[writeIndex++] = x + bottomY * width;
            }

            return path;
        }
    }
}
