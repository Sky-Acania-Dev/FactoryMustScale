namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    /// <summary>
    /// Deterministic event-queued conveyor/merger transport.
    /// - Tick N input phase applies events queued in tick N-1.
    /// - Tick N cell process resolves move intents from progress-ready source cells.
    /// - Tick N publish phase exposes resolved moves as events for tick N+1.
    /// </summary>
    public static class ConveyorArbitratedPropagationSystem
    {
        private const int DefaultFactoryPayloadItemChannel = 0;
        private const int DefaultTransportProgressThreshold = 1;

        public static void IngestEvents(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out int width, out int height, out int cellCount))
            {
                return;
            }

            EnsureBuffers(ref state, cellCount);

            int eventCount = state.ItemMoveEventCount;
            for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
            {
                int sourceIndex = state.ItemMoveEventSourceByIndex[eventIndex];
                int targetIndex = state.ItemMoveEventTargetByIndex[eventIndex];

                if (sourceIndex < 0 || sourceIndex >= cellCount || targetIndex < 0 || targetIndex >= cellCount)
                {
                    continue;
                }

                if (state.ItemPayloadByCell[sourceIndex] == 0 || state.ItemPayloadByCell[targetIndex] != 0)
                {
                    continue;
                }

                state.ItemPayloadByCell[targetIndex] = state.ItemPayloadByCell[sourceIndex];
                state.ItemPayloadByCell[sourceIndex] = 0;
                state.ItemTransportProgressByCell[sourceIndex] = 0;
                state.ItemTransportProgressByCell[targetIndex] = 0;
            }

            WritePayloadGrid(state.FactoryLayer, payloadChannel, width, height, state.ItemPayloadByCell);
            state.ItemMoveEventCount = 0;
        }

        public static void ProcessCells(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out int width, out int height, out int cellCount))
            {
                return;
            }

            EnsureBuffers(ref state, cellCount);
            ReadPayloadGrid(state.FactoryLayer, payloadChannel, width, height, state.ItemPayloadByCell);

            int progressThreshold = state.ItemTransportProgressThreshold > 0
                ? state.ItemTransportProgressThreshold
                : DefaultTransportProgressThreshold;

            for (int index = 0; index < cellCount; index++)
            {
                state.ItemIntentTargetBySource[index] = -1;
                state.ItemWinnerSourceByTarget[index] = -1;
                state.ItemWinnerCountByTarget[index] = 0;
            }

            for (int localY = 0; localY < height; localY++)
            {
                int y = state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = state.FactoryLayer.MinX + localX;
                    int sourceIndex = rowStart + localX;

                    if (!state.FactoryLayer.TryGet(x, y, out GridCellData sourceCell))
                    {
                        continue;
                    }

                    if (!CanOutputItems(sourceCell.StateId))
                    {
                        continue;
                    }

                    if (state.ItemPayloadByCell[sourceIndex] == 0)
                    {
                        state.ItemTransportProgressByCell[sourceIndex] = 0;
                        continue;
                    }

                    int nextProgress = state.ItemTransportProgressByCell[sourceIndex] + 1;
                    state.ItemTransportProgressByCell[sourceIndex] = nextProgress;
                    if (nextProgress < progressThreshold)
                    {
                        continue;
                    }

                    if (!TryGetOutputTarget(x, y, sourceCell.VariantId, out int targetX, out int targetY))
                    {
                        continue;
                    }

                    if (!state.FactoryLayer.IsInRange(targetX, targetY)
                        || !state.FactoryLayer.TryGet(targetX, targetY, out GridCellData targetCell)
                        || !CanAcceptItems(targetCell.StateId))
                    {
                        continue;
                    }

                    int targetLocalX = targetX - state.FactoryLayer.MinX;
                    int targetLocalY = targetY - state.FactoryLayer.MinY;
                    int targetIndex = (targetLocalY * width) + targetLocalX;

                    if (state.ItemPayloadByCell[targetIndex] != 0)
                    {
                        continue;
                    }

                    state.ItemIntentTargetBySource[sourceIndex] = targetIndex;
                    state.ItemWinnerCountByTarget[targetIndex]++;

                    int existingWinner = state.ItemWinnerSourceByTarget[targetIndex];
                    if (existingWinner < 0 || sourceIndex < existingWinner)
                    {
                        state.ItemWinnerSourceByTarget[targetIndex] = sourceIndex;
                    }
                }
            }

            ResolveMergerRoundRobinWinners(ref state, width, height, cellCount);
        }

        public static void PublishEvents(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out int width, out int height, out int cellCount))
            {
                state.NextItemMoveEventCount = 0;
                return;
            }

            EnsureBuffers(ref state, cellCount);

            int nextEventCount = 0;
            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int sourceIndex = state.ItemWinnerSourceByTarget[targetIndex];
                if (sourceIndex < 0 || state.ItemIntentTargetBySource[sourceIndex] != targetIndex)
                {
                    continue;
                }

                if (nextEventCount >= state.NextItemMoveEventSourceByIndex.Length)
                {
                    break;
                }

                state.NextItemMoveEventSourceByIndex[nextEventCount] = sourceIndex;
                state.NextItemMoveEventTargetByIndex[nextEventCount] = targetIndex;
                state.ItemTransportProgressByCell[sourceIndex] = 0;
                nextEventCount++;
            }

            for (int eventIndex = 0; eventIndex < nextEventCount; eventIndex++)
            {
                state.ItemMoveEventSourceByIndex[eventIndex] = state.NextItemMoveEventSourceByIndex[eventIndex];
                state.ItemMoveEventTargetByIndex[eventIndex] = state.NextItemMoveEventTargetByIndex[eventIndex];
            }

            state.ItemMoveEventCount = nextEventCount;
            state.NextItemMoveEventCount = 0;
        }

        private static int ResolvePayloadChannel(ref FactoryCoreLoopState state)
        {
            int payloadChannel = state.FactoryPayloadItemChannelIndex;
            if (payloadChannel < 0)
            {
                payloadChannel = DefaultFactoryPayloadItemChannel;
            }

            return payloadChannel;
        }

        private static bool TryGetLayerShape(Layer layer, int payloadChannel, out int width, out int height, out int cellCount)
        {
            width = 0;
            height = 0;
            cellCount = 0;

            if (layer == null || payloadChannel >= layer.PayloadChannelCount)
            {
                return false;
            }

            width = layer.Width;
            height = layer.Height;
            cellCount = width * height;
            return true;
        }

        private static void EnsureBuffers(ref FactoryCoreLoopState state, int cellCount)
        {
            if (state.ItemPayloadByCell == null || state.ItemPayloadByCell.Length != cellCount)
            {
                state.ItemPayloadByCell = new int[cellCount];
            }

            if (state.ItemTransportProgressByCell == null || state.ItemTransportProgressByCell.Length != cellCount)
            {
                state.ItemTransportProgressByCell = new int[cellCount];
            }

            if (state.ItemIntentTargetBySource == null || state.ItemIntentTargetBySource.Length != cellCount)
            {
                state.ItemIntentTargetBySource = new int[cellCount];
            }

            if (state.ItemWinnerSourceByTarget == null || state.ItemWinnerSourceByTarget.Length != cellCount)
            {
                state.ItemWinnerSourceByTarget = new int[cellCount];
            }

            if (state.ItemWinnerCountByTarget == null || state.ItemWinnerCountByTarget.Length != cellCount)
            {
                state.ItemWinnerCountByTarget = new int[cellCount];
            }

            if (state.ItemMergerRoundRobinCursorByCell == null || state.ItemMergerRoundRobinCursorByCell.Length != cellCount)
            {
                state.ItemMergerRoundRobinCursorByCell = new int[cellCount];
            }

            if (state.ItemMoveEventSourceByIndex == null || state.ItemMoveEventSourceByIndex.Length != cellCount)
            {
                state.ItemMoveEventSourceByIndex = new int[cellCount];
            }

            if (state.ItemMoveEventTargetByIndex == null || state.ItemMoveEventTargetByIndex.Length != cellCount)
            {
                state.ItemMoveEventTargetByIndex = new int[cellCount];
            }

            if (state.NextItemMoveEventSourceByIndex == null || state.NextItemMoveEventSourceByIndex.Length != cellCount)
            {
                state.NextItemMoveEventSourceByIndex = new int[cellCount];
            }

            if (state.NextItemMoveEventTargetByIndex == null || state.NextItemMoveEventTargetByIndex.Length != cellCount)
            {
                state.NextItemMoveEventTargetByIndex = new int[cellCount];
            }
        }

        private static void ResolveMergerRoundRobinWinners(ref FactoryCoreLoopState state, int width, int height, int cellCount)
        {
            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                if (state.ItemWinnerCountByTarget[targetIndex] <= 1)
                {
                    continue;
                }

                int targetLocalY = targetIndex / width;
                int targetLocalX = targetIndex - (targetLocalY * width);
                int targetX = state.FactoryLayer.MinX + targetLocalX;
                int targetY = state.FactoryLayer.MinY + targetLocalY;

                if (!state.FactoryLayer.TryGet(targetX, targetY, out GridCellData targetCell)
                    || targetCell.StateId != (int)GridStateId.Merger)
                {
                    continue;
                }

                int contenderCount = 0;
                for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
                {
                    if (state.ItemIntentTargetBySource[sourceIndex] == targetIndex)
                    {
                        contenderCount++;
                    }
                }

                if (contenderCount <= 1)
                {
                    continue;
                }

                int cursor = state.ItemMergerRoundRobinCursorByCell[targetIndex] % contenderCount;
                if (cursor < 0)
                {
                    cursor = 0;
                }

                int selectedSource = -1;
                int contenderOrdinal = 0;
                for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
                {
                    if (state.ItemIntentTargetBySource[sourceIndex] != targetIndex)
                    {
                        continue;
                    }

                    if (contenderOrdinal == cursor)
                    {
                        selectedSource = sourceIndex;
                        break;
                    }

                    contenderOrdinal++;
                }

                if (selectedSource >= 0)
                {
                    state.ItemWinnerSourceByTarget[targetIndex] = selectedSource;
                    state.ItemMergerRoundRobinCursorByCell[targetIndex] = cursor + 1;
                }
            }
        }

        private static bool CanOutputItems(int stateId)
        {
            return stateId == (int)GridStateId.Conveyor || stateId == (int)GridStateId.Merger;
        }

        private static bool CanAcceptItems(int stateId)
        {
            return stateId == (int)GridStateId.Conveyor || stateId == (int)GridStateId.Merger;
        }

        private static bool TryGetOutputTarget(int x, int y, int variantId, out int targetX, out int targetY)
        {
            targetX = x;
            targetY = y;

            CellOrientation orientation = GridCellData.GetOrientationEnum(variantId);
            switch (orientation)
            {
                case CellOrientation.Up:
                    targetY = y - 1;
                    return true;
                case CellOrientation.Right:
                    targetX = x + 1;
                    return true;
                case CellOrientation.Down:
                    targetY = y + 1;
                    return true;
                case CellOrientation.Left:
                    targetX = x - 1;
                    return true;
                default:
                    return false;
            }
        }

        private static void ReadPayloadGrid(Layer layer, int payloadChannel, int width, int height, int[] destination)
        {
            for (int localY = 0; localY < height; localY++)
            {
                int y = layer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = layer.MinX + localX;
                    int index = rowStart + localX;
                    layer.TryGetPayload(x, y, payloadChannel, out destination[index]);
                }
            }
        }

        private static void WritePayloadGrid(Layer layer, int payloadChannel, int width, int height, int[] source)
        {
            for (int localY = 0; localY < height; localY++)
            {
                int y = layer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = layer.MinX + localX;
                    int index = rowStart + localX;
                    layer.TrySetPayload(x, y, payloadChannel, source[index]);
                }
            }
        }
    }
}
