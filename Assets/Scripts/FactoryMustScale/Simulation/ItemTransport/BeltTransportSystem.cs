namespace FactoryMustScale.Simulation.ItemTransport
{
    using FactoryMustScale.Simulation;
    using FactoryMustScale.Simulation.Legacy;

    /// <summary>
    /// Deterministic belt-only transport with explicit PreCompute -> Compute -> Commit phases.
    ///
    /// Contract:
    /// - PreCompute: clear transient intent/resolution buffers and ingest queued external events.
    /// - Compute: build move intents from authoritative current buffers and resolve target conflicts
    ///   by lowest source index for deterministic winner selection.
    /// - Commit: apply two-buffer update (copy + progress advance, resolved moves, single swap).
    /// </summary>
    public static class BeltTransportSystem
    {
        private const int ProgressThreshold = 4;

        public static void PreCompute(ref FactoryCoreLoopState state)
        {
            if (!TryGetLayerShape(state.FactoryLayer, out int width, out int height, out int cellCount))
            {
                return;
            }

            BeltTransportBuffers.EnsureBuffers(ref state, cellCount);
            ClearTransient(ref state, cellCount);
            IngestCommands(ref state, width, height, cellCount);
        }

        public static void Compute(ref FactoryCoreLoopState state)
        {
            if (!TryGetLayerShape(state.FactoryLayer, out int width, out int height, out int cellCount))
            {
                return;
            }

            BeltTransportBuffers.EnsureBuffers(ref state, cellCount);

            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                if (!IsBeltCell(state.FactoryLayer.CellData[sourceIndex]) || state.ItemPayloadByCell[sourceIndex] == BeltTransportBuffers.Empty)
                {
                    continue;
                }

                if (state.ItemTransportProgressByCell[sourceIndex] < ProgressThreshold)
                {
                    continue;
                }

                int targetIndex = ComputeTargetIndex(sourceIndex, state.FactoryLayer.CellData[sourceIndex], width, height);
                if (targetIndex == BeltTransportBuffers.Invalid)
                {
                    continue;
                }

                if (!IsBeltCell(state.FactoryLayer.CellData[targetIndex]) || state.ItemPayloadByCell[targetIndex] != BeltTransportBuffers.Empty)
                {
                    continue;
                }

                state.ItemIntentTargetBySource[sourceIndex] = targetIndex;
            }

            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                int targetIndex = state.ItemIntentTargetBySource[sourceIndex];
                if (targetIndex == BeltTransportBuffers.Invalid)
                {
                    continue;
                }

                int winner = state.ItemResolvedSourceByTarget[targetIndex];
                if (winner == BeltTransportBuffers.Invalid || sourceIndex < winner)
                {
                    state.ItemResolvedSourceByTarget[targetIndex] = sourceIndex;
                }
            }

            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int winner = state.ItemResolvedSourceByTarget[targetIndex];
                if (winner != BeltTransportBuffers.Invalid)
                {
                    state.ItemResolvedTargetBySource[winner] = targetIndex;
                }
            }
        }

        public static void Commit(ref FactoryCoreLoopState state)
        {
            if (!TryGetLayerShape(state.FactoryLayer, out _, out _, out int cellCount))
            {
                return;
            }

            BeltTransportBuffers.EnsureBuffers(ref state, cellCount);

            for (int index = 0; index < cellCount; index++)
            {
                int item = state.ItemPayloadByCell[index];
                state.ItemNextPayloadByCell[index] = item;
                if (item == BeltTransportBuffers.Empty)
                {
                    state.ItemNextTransportProgressByCell[index] = 0;
                }
                else
                {
                    int nextProgress = state.ItemTransportProgressByCell[index] + 1;
                    state.ItemNextTransportProgressByCell[index] = nextProgress > ProgressThreshold ? ProgressThreshold : nextProgress;
                }
            }

            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int sourceIndex = state.ItemResolvedSourceByTarget[targetIndex];
                if (sourceIndex == BeltTransportBuffers.Invalid)
                {
                    continue;
                }

                int item = state.ItemPayloadByCell[sourceIndex];
                if (item == BeltTransportBuffers.Empty)
                {
                    continue;
                }

                state.ItemNextPayloadByCell[sourceIndex] = BeltTransportBuffers.Empty;
                state.ItemNextTransportProgressByCell[sourceIndex] = 0;
                state.ItemNextPayloadByCell[targetIndex] = item;
                state.ItemNextTransportProgressByCell[targetIndex] = 0;
            }

            int[] payload = state.ItemPayloadByCell;
            state.ItemPayloadByCell = state.ItemNextPayloadByCell;
            state.ItemNextPayloadByCell = payload;

            int[] progress = state.ItemTransportProgressByCell;
            state.ItemTransportProgressByCell = state.ItemNextTransportProgressByCell;
            state.ItemNextTransportProgressByCell = progress;
        }

        private static void IngestCommands(ref FactoryCoreLoopState state, int width, int height, int cellCount)
        {
            if (state.CommandCount <= 0 || state.CommandTypeByIndex == null || state.CommandCellIndexByIndex == null || state.CommandDirOrItemIdByIndex == null)
            {
                state.CommandCount = 0;
                return;
            }

            int commandCount = state.CommandCount;
            if (commandCount > state.CommandTypeByIndex.Length)
            {
                commandCount = state.CommandTypeByIndex.Length;
            }

            if (commandCount > state.CommandCellIndexByIndex.Length)
            {
                commandCount = state.CommandCellIndexByIndex.Length;
            }

            if (commandCount > state.CommandDirOrItemIdByIndex.Length)
            {
                commandCount = state.CommandDirOrItemIdByIndex.Length;
            }

            for (int i = 0; i < commandCount; i++)
            {
                if (state.CommandTypeByIndex[i] != 4)
                {
                    continue;
                }

                int cellIndex = state.CommandCellIndexByIndex[i];
                if (cellIndex < 0 || cellIndex >= cellCount)
                {
                    continue;
                }

                int x = cellIndex % width;
                int y = cellIndex / width;
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                if (!IsBeltCell(state.FactoryLayer.CellData[cellIndex]))
                {
                    continue;
                }

                if (state.ItemPayloadByCell[cellIndex] == BeltTransportBuffers.Empty)
                {
                    state.ItemPayloadByCell[cellIndex] = state.CommandDirOrItemIdByIndex[i];
                    state.ItemTransportProgressByCell[cellIndex] = 0;
                }
            }

            state.CommandCount = 0;
        }

        private static void ClearTransient(ref FactoryCoreLoopState state, int cellCount)
        {
            for (int i = 0; i < cellCount; i++)
            {
                state.ItemIntentTargetBySource[i] = BeltTransportBuffers.Invalid;
                state.ItemResolvedSourceByTarget[i] = BeltTransportBuffers.Invalid;
                state.ItemResolvedTargetBySource[i] = BeltTransportBuffers.Invalid;
            }
        }

        private static bool TryGetLayerShape(Layer layer, out int width, out int height, out int cellCount)
        {
            width = 0;
            height = 0;
            cellCount = 0;

            if (layer == null)
            {
                return false;
            }

            width = layer.Width;
            height = layer.Height;
            cellCount = width * height;
            return width > 0 && height > 0;
        }

        private static bool IsBeltCell(in GridCellData cell)
        {
            return cell.StateId == (int)GridStateId.Conveyor;
        }

        private static int ComputeTargetIndex(int sourceIndex, in GridCellData sourceCell, int width, int height)
        {
            int x = sourceIndex % width;
            int y = sourceIndex / width;
            int direction = GridCellData.GetOrientation(sourceCell.VariantId);

            switch (direction & 3)
            {
                case 0:
                    y -= 1;
                    break;
                case 1:
                    x += 1;
                    break;
                case 2:
                    y += 1;
                    break;
                case 3:
                    x -= 1;
                    break;
            }

            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return BeltTransportBuffers.Invalid;
            }

            return (y * width) + x;
        }
    }
}
