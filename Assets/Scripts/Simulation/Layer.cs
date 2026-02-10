using System;

namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Deterministic 2D integer-grid layer with preallocated storage.
    /// Optional payload channels are layer-owned arrays sharing the same cell index mapping.
    /// </summary>
    public sealed class Layer
    {
        private readonly int _minX;
        private readonly int _minY;
        private readonly int _width;
        private readonly int _height;
        private readonly GridCellData[] _cells;
        private readonly int[] _payloadValues;
        private readonly int _payloadChannelCount;

        public Layer(int minX, int minY, int width, int height, int payloadChannelCount = 0)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (payloadChannelCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadChannelCount));
            }

            int cellCount = checked(width * height);
            int payloadCount = checked(cellCount * payloadChannelCount);

            _minX = minX;
            _minY = minY;
            _width = width;
            _height = height;
            _cells = new GridCellData[cellCount];
            _payloadChannelCount = payloadChannelCount;
            _payloadValues = new int[payloadCount];
        }

        public int MinX => _minX;
        public int MinY => _minY;
        public int Width => _width;
        public int Height => _height;
        public int PayloadChannelCount => _payloadChannelCount;

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

        /// <summary>
        /// Writes an optional layer-owned payload channel value for one cell.
        /// </summary>
        public bool TrySetPayload(int x, int y, int channelIndex, int payloadValue)
        {
            int payloadIndex;
            if (!TryGetPayloadIndex(x, y, channelIndex, out payloadIndex))
            {
                return false;
            }

            _payloadValues[payloadIndex] = payloadValue;
            return true;
        }

        /// <summary>
        /// Reads an optional layer-owned payload channel value for one cell.
        /// </summary>
        public bool TryGetPayload(int x, int y, int channelIndex, out int payloadValue)
        {
            int payloadIndex;
            if (!TryGetPayloadIndex(x, y, channelIndex, out payloadIndex))
            {
                payloadValue = default;
                return false;
            }

            payloadValue = _payloadValues[payloadIndex];
            return true;
        }

        private bool TryGetPayloadIndex(int x, int y, int channelIndex, out int payloadIndex)
        {
            if (channelIndex < 0 || channelIndex >= _payloadChannelCount)
            {
                payloadIndex = -1;
                return false;
            }

            int cellIndex;
            if (!TryGetIndex(x, y, out cellIndex))
            {
                payloadIndex = -1;
                return false;
            }

            payloadIndex = (cellIndex * _payloadChannelCount) + channelIndex;
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
