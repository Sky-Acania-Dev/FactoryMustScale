using FactoryMustScale.Simulation;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Deterministic runtime harness used by DebugGridMinimapView.
    /// This is intentionally minimal debug scaffolding (not production simulation systems).
    /// </summary>
    public sealed class DebugGridMinimapTestScenario
    {
        private const int ProducedItemId = 1;
        private readonly int[] _pathCellIndices;
        private readonly int _minerOutputIndex;
        private readonly int _storageInputIndex;
        private readonly int _minerPeriodTicks;

        private int _tick;
        private int _storedCount;

        public DebugGridMinimapTestScenario(int width, int height, GridCellData[] cells, int[] itemIdByCell, int[] pathCellIndices, int minerPeriodTicks)
        {
            Width = width;
            Height = height;
            Cells = cells;
            ItemIdByCell = itemIdByCell;
            _pathCellIndices = pathCellIndices;
            _minerPeriodTicks = minerPeriodTicks < 1 ? 1 : minerPeriodTicks;
            _minerOutputIndex = pathCellIndices[0];
            _storageInputIndex = pathCellIndices[pathCellIndices.Length - 1];
        }

        public int Width { get; }
        public int Height { get; }
        public GridCellData[] Cells { get; }
        public int[] ItemIdByCell { get; }
        public int StoredCount => _storedCount;

        public void Tick(int ticks)
        {
            int boundedTicks = ticks < 0 ? 0 : ticks;
            for (int i = 0; i < boundedTicks; i++)
            {
                StepOnce();
            }
        }

        private void StepOnce()
        {
            _tick++;

            for (int i = _pathCellIndices.Length - 2; i >= 0; i--)
            {
                int fromIndex = _pathCellIndices[i];
                int toIndex = _pathCellIndices[i + 1];

                if (toIndex == _storageInputIndex)
                {
                    int payloadAtStorageInput = ItemIdByCell[toIndex];
                    if (payloadAtStorageInput != 0)
                    {
                        _storedCount += 1;
                        ItemIdByCell[toIndex] = 0;
                    }
                }

                int payload = ItemIdByCell[fromIndex];
                if (payload == 0 || ItemIdByCell[toIndex] != 0)
                {
                    continue;
                }

                ItemIdByCell[toIndex] = payload;
                ItemIdByCell[fromIndex] = 0;
            }

            if ((_tick % _minerPeriodTicks) == 0 && ItemIdByCell[_minerOutputIndex] == 0)
            {
                ItemIdByCell[_minerOutputIndex] = ProducedItemId;
            }
        }

        public static DebugGridMinimapTestScenario CreateDefaultSShape()
        {
            const int width = 32;
            const int height = 16;
            const int minerPeriodTicks = 6;

            int cellCount = width * height;
            var cells = new GridCellData[cellCount];
            var itemIdByCell = new int[cellCount];

            int[] path = BuildSPath(width);
            for (int i = 0; i < path.Length; i++)
            {
                int cellIndex = path[i];
                GridCellData cell = cells[cellIndex];
                cell.StateId = (int)GridStateId.Conveyor;
                cells[cellIndex] = cell;
            }

            GridCellData minerCell = cells[path[0]];
            minerCell.StateId = (int)GridStateId.Miner;
            cells[path[0]] = minerCell;

            GridCellData storageCell = cells[path[path.Length - 1]];
            storageCell.StateId = (int)GridStateId.Storage;
            cells[path[path.Length - 1]] = storageCell;

            return new DebugGridMinimapTestScenario(width, height, cells, itemIdByCell, path, minerPeriodTicks);
        }

        private static int[] BuildSPath(int width)
        {
            const int left = 2;
            const int right = 28;
            const int topY = 12;
            const int midY = 8;
            const int bottomY = 4;

            int length = (right - left + 1) + (topY - midY) + (right - left) + (midY - bottomY) + (right - left);
            var path = new int[length];
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
