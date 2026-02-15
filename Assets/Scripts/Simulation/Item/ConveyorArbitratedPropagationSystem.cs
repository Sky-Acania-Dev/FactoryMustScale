namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    /// <summary>
    /// Deterministic conveyor payload movement with source-target arbitration and same-tick propagation.
    /// </summary>
    public static class ConveyorArbitratedPropagationSystem
    {
        private const int DefaultFactoryPayloadItemChannel = 0;

        public static void Run(ref FactoryCoreLoopState state)
        {
            int payloadChannel = state.FactoryPayloadItemChannelIndex;
            if (payloadChannel < 0)
            {
                payloadChannel = DefaultFactoryPayloadItemChannel;
            }

            if (state.FactoryLayer == null || payloadChannel >= state.FactoryLayer.PayloadChannelCount)
            {
                return;
            }

            int width = state.FactoryLayer.Width;
            int height = state.FactoryLayer.Height;
            int cellCount = width * height;

            EnsureBuffers(ref state, cellCount);

            for (int localY = 0; localY < height; localY++)
            {
                int y = state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = state.FactoryLayer.MinX + localX;
                    int sourceIndex = rowStart + localX;

                    state.FactoryLayer.TryGetPayload(x, y, payloadChannel, out int payloadValue);
                    state.ItemPayloadRead[sourceIndex] = payloadValue;
                    state.ItemPayloadWrite[sourceIndex] = payloadValue;
                    state.ItemIntentTargetBySource[sourceIndex] = -1;
                    state.ItemWinnerSourceByTarget[sourceIndex] = -1;
                    state.ItemWinningTargetBySource[sourceIndex] = -1;
                    state.ItemCanExecuteMoveBySource[sourceIndex] = 0;
                }
            }

            for (int localY = 0; localY < height; localY++)
            {
                int y = state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = state.FactoryLayer.MinX + localX;
                    int sourceIndex = rowStart + localX;

                    if (state.ItemPayloadRead[sourceIndex] == 0)
                    {
                        continue;
                    }

                    if (!state.FactoryLayer.TryGet(x, y, out GridCellData sourceCell)
                        || sourceCell.StateId != (int)GridStateId.Conveyor)
                    {
                        continue;
                    }

                    if (!TryGetConveyorTarget(x, y, sourceCell.VariantId, out int targetX, out int targetY))
                    {
                        continue;
                    }

                    if (!state.FactoryLayer.IsInRange(targetX, targetY)
                        || !state.FactoryLayer.TryGet(targetX, targetY, out GridCellData targetCell)
                        || targetCell.StateId != (int)GridStateId.Conveyor)
                    {
                        continue;
                    }

                    int targetLocalX = targetX - state.FactoryLayer.MinX;
                    int targetLocalY = targetY - state.FactoryLayer.MinY;
                    int targetIndex = (targetLocalY * width) + targetLocalX;

                    state.ItemIntentTargetBySource[sourceIndex] = targetIndex;

                    int existingWinner = state.ItemWinnerSourceByTarget[targetIndex];
                    if (existingWinner < 0 || sourceIndex < existingWinner)
                    {
                        state.ItemWinnerSourceByTarget[targetIndex] = sourceIndex;
                    }
                }
            }

            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int winningSource = state.ItemWinnerSourceByTarget[targetIndex];
                if (winningSource >= 0 && state.ItemIntentTargetBySource[winningSource] == targetIndex)
                {
                    state.ItemWinningTargetBySource[winningSource] = targetIndex;
                }
            }

            PropagateExecutableMoves(ref state, cellCount);

            int traversalStamp = 1;
            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                if (state.ItemCanExecuteMoveBySource[sourceIndex] != 0)
                {
                    continue;
                }

                if (state.ItemWinningTargetBySource[sourceIndex] < 0)
                {
                    continue;
                }

                int current = sourceIndex;
                while (true)
                {
                    if (state.ItemCanExecuteMoveBySource[current] != 0)
                    {
                        break;
                    }

                    int targetIndex = state.ItemWinningTargetBySource[current];
                    if (targetIndex < 0)
                    {
                        break;
                    }

                    if (state.ItemPayloadRead[targetIndex] == 0)
                    {
                        break;
                    }

                    if (state.ItemWinningTargetBySource[targetIndex] < 0)
                    {
                        break;
                    }

                    if (state.ItemVisitStampBySource[current] == traversalStamp)
                    {
                        int cycleStart = current;
                        int cycleNode = cycleStart;

                        while (true)
                        {
                            state.ItemCanExecuteMoveBySource[cycleNode] = 1;
                            cycleNode = state.ItemWinningTargetBySource[cycleNode];

                            if (cycleNode == cycleStart)
                            {
                                break;
                            }
                        }

                        break;
                    }

                    state.ItemVisitStampBySource[current] = traversalStamp;
                    current = targetIndex;
                }

                traversalStamp++;
            }

            PropagateExecutableMoves(ref state, cellCount);

            // Commit in two passes to avoid chain-overwrite bugs.
            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                if (state.ItemCanExecuteMoveBySource[sourceIndex] != 0)
                {
                    state.ItemPayloadWrite[sourceIndex] = 0;
                }
            }

            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                if (state.ItemCanExecuteMoveBySource[sourceIndex] == 0)
                {
                    continue;
                }

                int targetIndex = state.ItemWinningTargetBySource[sourceIndex];
                if (targetIndex < 0)
                {
                    continue;
                }

                state.ItemPayloadWrite[targetIndex] = state.ItemPayloadRead[sourceIndex];
            }

            for (int localY = 0; localY < height; localY++)
            {
                int y = state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = state.FactoryLayer.MinX + localX;
                    int index = rowStart + localX;
                    state.FactoryLayer.TrySetPayload(x, y, payloadChannel, state.ItemPayloadWrite[index]);
                }
            }
        }

        private static void EnsureBuffers(ref FactoryCoreLoopState state, int cellCount)
        {
            if (state.ItemPayloadRead == null || state.ItemPayloadRead.Length != cellCount)
            {
                state.ItemPayloadRead = new int[cellCount];
            }

            if (state.ItemPayloadWrite == null || state.ItemPayloadWrite.Length != cellCount)
            {
                state.ItemPayloadWrite = new int[cellCount];
            }

            if (state.ItemIntentTargetBySource == null || state.ItemIntentTargetBySource.Length != cellCount)
            {
                state.ItemIntentTargetBySource = new int[cellCount];
            }

            if (state.ItemWinnerSourceByTarget == null || state.ItemWinnerSourceByTarget.Length != cellCount)
            {
                state.ItemWinnerSourceByTarget = new int[cellCount];
            }

            if (state.ItemWinningTargetBySource == null || state.ItemWinningTargetBySource.Length != cellCount)
            {
                state.ItemWinningTargetBySource = new int[cellCount];
            }

            if (state.ItemCanExecuteMoveBySource == null || state.ItemCanExecuteMoveBySource.Length != cellCount)
            {
                state.ItemCanExecuteMoveBySource = new byte[cellCount];
            }

            if (state.ItemVisitStampBySource == null || state.ItemVisitStampBySource.Length != cellCount)
            {
                state.ItemVisitStampBySource = new int[cellCount];
            }
        }

        private static void PropagateExecutableMoves(ref FactoryCoreLoopState state, int cellCount)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;

                for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
                {
                    if (state.ItemCanExecuteMoveBySource[sourceIndex] != 0)
                    {
                        continue;
                    }

                    int targetIndex = state.ItemWinningTargetBySource[sourceIndex];
                    if (targetIndex < 0)
                    {
                        continue;
                    }

                    if (state.ItemPayloadRead[targetIndex] == 0)
                    {
                        state.ItemCanExecuteMoveBySource[sourceIndex] = 1;
                        changed = true;
                        continue;
                    }

                    if (state.ItemWinningTargetBySource[targetIndex] >= 0
                        && state.ItemCanExecuteMoveBySource[targetIndex] != 0)
                    {
                        state.ItemCanExecuteMoveBySource[sourceIndex] = 1;
                        changed = true;
                    }
                }
            }
        }

        private static bool TryGetConveyorTarget(int x, int y, int variantId, out int targetX, out int targetY)
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
    }
}
