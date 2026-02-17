namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    public static class ConveyorArbitratedPropagationSystem
    {
        private const int DefaultFactoryPayloadItemChannel = 0;
        private const int DefaultTransportProgressThreshold = 1;
        private const int DefaultGeneratedItemType = 1;

        public static void IngestEvents(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out _, out _, out int cellCount))
            {
                return;
            }

            EnsureBuffers(ref state, cellCount);

            int eventCount = state.SimEvents.WorkingCount;
            for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
            {
                if (!state.SimEvents.TryGetWorkingEvent(eventIndex, out SimEvent simEvent) || simEvent.Id != SimEventId.ItemTransported)
                {
                    continue;
                }

                SimMutations.TryApplyTransport(state.FactoryLayer, payloadChannel, simEvent, state.FactoryTicksExecuted, ref state.SimEvents);

                if (simEvent.SourceIndex >= 0 && simEvent.SourceIndex < cellCount)
                {
                    state.ItemTransportProgressByCell[simEvent.SourceIndex] = 0;
                }

                if (simEvent.TargetIndex >= 0 && simEvent.TargetIndex < cellCount)
                {
                    state.ItemTransportProgressByCell[simEvent.TargetIndex] = 0;
                }
            }
        }

        public static void ProcessCells(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out int width, out int height, out int cellCount))
            {
                return;
            }

            EnsureBuffers(ref state, cellCount);
            EnsureStorageBuffer(ref state, cellCount);
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

            int tickIndex = state.FactoryTicksExecuted;

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

                    ApplyLifecycleByCell(
                        ref state,
                        sourceCell.StateId,
                        sourceIndex,
                        payloadChannel,
                        tickIndex);

                    int sourcePayload = state.ItemPayloadByCell[sourceIndex];
                    if (!CanOutputItems(sourceCell.StateId))
                    {
                        continue;
                    }

                    if (sourcePayload == 0)
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

            ResolveMergerRoundRobinWinners(ref state, width, cellCount);
        }

        public static void PublishEvents(ref FactoryCoreLoopState state)
        {
            int payloadChannel = ResolvePayloadChannel(ref state);
            if (!TryGetLayerShape(state.FactoryLayer, payloadChannel, out _, out _, out int cellCount))
            {
                state.ItemMoveEventCount = 0;
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

                if (nextEventCount >= state.ItemMoveEventSourceByIndex.Length)
                {
                    break;
                }

                state.ItemMoveEventSourceByIndex[nextEventCount] = sourceIndex;
                state.ItemMoveEventTargetByIndex[nextEventCount] = targetIndex;
                state.ItemTransportProgressByCell[sourceIndex] = 0;
                nextEventCount++;

                SimMutations.QueueTransportForNextTick(
                    state.FactoryTicksExecuted,
                    sourceIndex,
                    targetIndex,
                    SimEventEndpointKind.Cell,
                    SimEventEndpointKind.Cell,
                    state.ItemPayloadByCell[sourceIndex],
                    ref state.SimEvents);
            }

            state.ItemMoveEventCount = nextEventCount;
        }

        private static void ApplyLifecycleByCell(
            ref FactoryCoreLoopState state,
            int stateId,
            int cellIndex,
            int payloadChannel,
            int tickIndex)
        {
            if (stateId == (int)GridStateId.Miner)
            {
                if (state.ItemPayloadByCell[cellIndex] == 0
                    && SimMutations.TryGenerateItem(
                        state.FactoryLayer,
                        payloadChannel,
                        cellIndex,
                        tickIndex,
                        DefaultGeneratedItemType,
                        ref state.SimEvents))
                {
                    state.ItemPayloadByCell[cellIndex] = DefaultGeneratedItemType;
                }

                return;
            }

            if (stateId == (int)GridStateId.CrafterCore)
            {
                int sourcePayload = state.ItemPayloadByCell[cellIndex];
                if (sourcePayload != 0)
                {
                    int mutatedPayload = sourcePayload + 100;
                    if (SimMutations.TryMutateItem(
                        state.FactoryLayer,
                        payloadChannel,
                        cellIndex,
                        tickIndex,
                        mutatedPayload,
                        ref state.SimEvents))
                    {
                        state.ItemPayloadByCell[cellIndex] = mutatedPayload;
                    }
                }

                return;
            }

            if (stateId == (int)GridStateId.Storage
                && state.ItemPayloadByCell[cellIndex] != 0
                && SimMutations.TryStoreItem(
                    state.FactoryLayer,
                    payloadChannel,
                    cellIndex,
                    cellIndex,
                    tickIndex,
                    state.StorageItemCountByCell,
                    ref state.SimEvents))
            {
                state.ItemPayloadByCell[cellIndex] = 0;
            }
        }

        private static int ResolvePayloadChannel(ref FactoryCoreLoopState state)
        {
            return state.FactoryPayloadItemChannelIndex < 0
                ? DefaultFactoryPayloadItemChannel
                : state.FactoryPayloadItemChannelIndex;
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

        private static void EnsureStorageBuffer(ref FactoryCoreLoopState state, int cellCount)
        {
            if (state.StorageItemCountByCell == null || state.StorageItemCountByCell.Length != cellCount)
            {
                state.StorageItemCountByCell = new int[cellCount];
            }
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
        }

        private static void ResolveMergerRoundRobinWinners(ref FactoryCoreLoopState state, int width, int cellCount)
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
            return stateId == (int)GridStateId.Conveyor
                || stateId == (int)GridStateId.Merger
                || stateId == (int)GridStateId.Miner;
        }

        private static bool CanAcceptItems(int stateId)
        {
            return stateId == (int)GridStateId.Conveyor
                || stateId == (int)GridStateId.Merger
                || stateId == (int)GridStateId.Storage;
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
    }
}
