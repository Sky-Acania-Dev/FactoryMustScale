namespace FactoryMustScale.Simulation.Item
{
    using FactoryMustScale.Simulation.Core;

    public static class FactoryItemLifecycleSystem
    {
        private const int DefaultGeneratedItemType = 1;

        public static void Run(ref FactoryCoreLoopState state, int tickIndex)
        {
            if (state.FactoryLayer == null)
            {
                return;
            }

            int payloadChannel = state.FactoryPayloadItemChannelIndex >= 0 ? state.FactoryPayloadItemChannelIndex : 0;
            int width = state.FactoryLayer.Width;
            int height = state.FactoryLayer.Height;
            int cellCount = width * height;

            if (state.StorageItemCountByCell == null || state.StorageItemCountByCell.Length != cellCount)
            {
                state.StorageItemCountByCell = new int[cellCount];
            }

            for (int localY = 0; localY < height; localY++)
            {
                int y = state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = state.FactoryLayer.MinX + localX;
                    int cellIndex = rowStart + localX;

                    if (!state.FactoryLayer.TryGet(x, y, out GridCellData cell))
                    {
                        continue;
                    }

                    if (cell.StateId == (int)GridStateId.Miner)
                    {
                        if (state.FactoryLayer.TryGetPayload(x, y, payloadChannel, out int payload) && payload == 0)
                        {
                            SimMutations.TryGenerateItem(state.FactoryLayer, payloadChannel, cellIndex, tickIndex, DefaultGeneratedItemType, ref state.SimEvents);
                        }

                        continue;
                    }

                    if (cell.StateId == (int)GridStateId.CrafterCore)
                    {
                        if (state.FactoryLayer.TryGetPayload(x, y, payloadChannel, out int payload) && payload != 0)
                        {
                            SimMutations.TryMutateItem(state.FactoryLayer, payloadChannel, cellIndex, tickIndex, payload + 100, ref state.SimEvents);
                        }

                        continue;
                    }

                    if (cell.StateId == (int)GridStateId.Storage)
                    {
                        SimMutations.TryStoreItem(state.FactoryLayer, payloadChannel, cellIndex, cellIndex, tickIndex, state.StorageItemCountByCell, ref state.SimEvents);
                    }
                }
            }
        }
    }
}
