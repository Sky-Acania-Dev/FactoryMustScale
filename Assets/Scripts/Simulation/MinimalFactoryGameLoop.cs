namespace FactoryMustScale.Simulation
{
    public enum MinimalFactoryGamePhase : byte
    {
        PendingInitialization = 0,
        Running = 1,
        Ended = 2,
    }

    public struct MinimalFactoryGameState
    {
        public int Seed;
        public int TerrainResourceChannelIndex;
        public int MaxFactoryTicks;
        public int FactoryTicksExecuted;
        public MinimalFactoryGamePhase Phase;
        public Layer TerrainLayer;
        public Layer FactoryLayer;
    }

    /// <summary>
    /// Small deterministic game-loop system for early integration testing.
    /// Sequence:
    /// 1) Terrain generation (weighted deterministic layout)
    /// 2) Factory layer initialization
    /// 3) Fixed-step factory loop progression
    /// 4) End game when max tick budget is reached
    /// </summary>
    public struct MinimalFactoryGameLoopSystem : ISimulationSystem<MinimalFactoryGameState>
    {
        private const int TerrainRollMod = 100;
        private const int GroundThreshold = 60;
        private const int ResourceDepositThreshold = 78;
        private const int WaterThreshold = 90;
        private const int CliffThreshold = 97;
        private const int BlockedThreshold = 99;

        public void Tick(ref MinimalFactoryGameState state, int tickIndex)
        {
            if (state.Phase == MinimalFactoryGamePhase.PendingInitialization)
            {
                InitializeGame(ref state);
            }

            if (state.Phase != MinimalFactoryGamePhase.Running)
            {
                return;
            }

            state.FactoryTicksExecuted++;

            if (state.FactoryTicksExecuted >= state.MaxFactoryTicks)
            {
                state.Phase = MinimalFactoryGamePhase.Ended;
            }
        }

        private static void InitializeGame(ref MinimalFactoryGameState state)
        {
            if (state.TerrainLayer == null || state.FactoryLayer == null)
            {
                state.Phase = MinimalFactoryGamePhase.Ended;
                return;
            }

            for (int localY = 0; localY < state.TerrainLayer.Height; localY++)
            {
                int y = state.TerrainLayer.MinY + localY;

                for (int localX = 0; localX < state.TerrainLayer.Width; localX++)
                {
                    int x = state.TerrainLayer.MinX + localX;

                    TerrainType terrainType = SelectTerrainType(state.Seed, x, y);
                    ResourceType resourceType = SelectResourceType(terrainType);

                    state.TerrainLayer.TrySetCellState(x, y, (int)terrainType, 0, 0u, currentTick: 0, out _);
                    state.TerrainLayer.TrySetPayload(x, y, state.TerrainResourceChannelIndex, (int)resourceType);

                    state.FactoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            state.FactoryTicksExecuted = 0;
            state.Phase = MinimalFactoryGamePhase.Running;
        }

        private static TerrainType SelectTerrainType(int seed, int x, int y)
        {
            int hash = DeterministicHash(seed, x, y);
            int roll = hash % TerrainRollMod;

            if (roll < GroundThreshold)
            {
                return TerrainType.Ground;
            }

            if (roll < ResourceDepositThreshold)
            {
                return TerrainType.ResourceDeposit;
            }

            if (roll < WaterThreshold)
            {
                return TerrainType.Water;
            }

            if (roll < CliffThreshold)
            {
                return TerrainType.Cliff;
            }

            if (roll < BlockedThreshold)
            {
                return TerrainType.Blocked;
            }

            return TerrainType.GeothermalSite;
        }

        private static ResourceType SelectResourceType(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.ResourceDeposit:
                    return ResourceType.Ore;
                case TerrainType.GeothermalSite:
                    return ResourceType.Geothermal;
                default:
                    return ResourceType.None;
            }
        }

        private static int DeterministicHash(int seed, int x, int y)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= 2166136261u;
                value = (value ^ (uint)(x * 374761393)) * 16777619u;
                value = (value ^ (uint)(y * 668265263)) * 16777619u;
                value ^= value >> 13;
                value *= 1274126177u;
                value ^= value >> 16;
                return (int)(value & 0x7FFF_FFFFu);
            }
        }
    }
}
