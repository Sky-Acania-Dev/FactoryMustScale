using System;

namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Deterministic 2D integer-grid layer with preallocated storage.
    ///
    /// Input contract:
    /// - event position (x, y)
    /// - event payload (int)
    ///
    /// Output contract:
    /// - layer mutates its own state in place
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

        public bool TryApplyEvent(int x, int y, int eventValue, out GridCellData updatedData)
        {
            int index;
            if (!TryGetIndex(x, y, out index))
            {
                updatedData = default;
                return false;
            }

            GridCellData cellData = _cells[index];
            cellData.LastEventValue = eventValue;
            cellData.EventCount++;
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
