using System;

namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Deterministic 2D integer-grid layer with preallocated storage.
    /// </summary>
    public sealed class Layer
    {
        private readonly int _minX;
        private readonly int _minY;
        private readonly int _width;
        private readonly int _height;
        private readonly GridCellData[] _cells;

        public Layer(int minX, int minY, int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            _minX = minX;
            _minY = minY;
            _width = width;
            _height = height;
            _cells = new GridCellData[width * height];
        }

        public int MinX => _minX;
        public int MinY => _minY;
        public int Width => _width;
        public int Height => _height;

        public bool IsInRange(int x, int y)
        {
            int localX = x - _minX;
            int localY = y - _minY;

            return localX >= 0 && localX < _width && localY >= 0 && localY < _height;
        }

        public bool TryGet(int x, int y, out GridCellData data)
        {
            int index;
            if (!TryGetIndex(x, y, out index))
            {
                data = default;
                return false;
            }

            data = _cells[index];
            return true;
        }

        /// <summary>
        /// Applies one deterministic state-change event to a cell.
        /// Supports conveyor/crafter modeling by allowing state and variant to differ per cell.
        /// </summary>
        public bool TrySetCellState(
            int x,
            int y,
            int stateId,
            int variantId,
            uint flags,
            int currentTick,
            out GridCellData updatedData)
        {
            int index;
            if (!TryGetIndex(x, y, out index))
            {
                updatedData = default;
                return false;
            }

            GridCellData cellData = _cells[index];
            cellData.StateId = stateId;
            cellData.VariantId = variantId;
            cellData.Flags = flags;
            cellData.LastUpdatedTick = currentTick;
            cellData.ChangeCount++;
            _cells[index] = cellData;

            updatedData = cellData;
            return true;
        }

        private bool TryGetIndex(int x, int y, out int index)
        {
            int localX = x - _minX;
            int localY = y - _minY;

            if (localX < 0 || localX >= _width || localY < 0 || localY >= _height)
            {
                index = -1;
                return false;
            }

            index = localY * _width + localX;
            return true;
        }
    }
}
