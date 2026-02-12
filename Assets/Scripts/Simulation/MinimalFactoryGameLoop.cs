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
        public BuildableRuleData[] BuildableRules;
        public FactoryCommandQueue CommandQueue;
        public FactoryCommandResultBuffer CommandResults;
        public FactoryFootprintData[] Footprints;
        public bool StopProcessingOnFailure;
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

            state.CommandResults.Clear();
            ApplyQueuedCommands(ref state, tickIndex);
            state.CommandQueue.Clear();

            state.FactoryTicksExecuted++;

            if (state.FactoryTicksExecuted >= state.MaxFactoryTicks)
            {
                state.Phase = MinimalFactoryGamePhase.Ended;
            }
        }

        private static void ApplyQueuedCommands(ref MinimalFactoryGameState state, int tickIndex)
        {
            int commandCount = state.CommandQueue.Count;

            for (int i = 0; i < commandCount; i++)
            {
                FactoryCommand command = state.CommandQueue.GetAt(i);
                FactoryCommandResult result = BuildDefaultResult(i, command);

                switch (command.Type)
                {
                    case FactoryCommandType.PlaceBuilding:
                        ApplyPlaceCommand(ref state, command, tickIndex, ref result);
                        break;
                    case FactoryCommandType.RemoveBuilding:
                        ApplyRemoveCommand(ref state, command, tickIndex, ref result);
                        break;
                    case FactoryCommandType.RotateBuilding:
                        ApplyRotateCommand(ref state, command, tickIndex, ref result);
                        break;
                    case FactoryCommandType.MoveBuilding:
                        ApplyMoveCommand(ref state, command, tickIndex, ref result);
                        break;
                    default:
                        result.Success = false;
                        result.FailureReason = FactoryCommandFailureReason.UnknownCommand;
                        break;
                }

                state.CommandResults.TryAdd(result);

                if (!result.Success && state.StopProcessingOnFailure)
                {
                    break;
                }
            }
        }

        private static void ApplyPlaceCommand(ref MinimalFactoryGameState state, FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            BuildableRuleData buildRule;
            if (!BuildableRules.TryGetRule(state.BuildableRules, command.StateId, out buildRule))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.MissingBuildRule;
                return;
            }

            bool hasIrregularFootprint = TryGetFootprint(state.Footprints, command.FootprintId, out FactoryFootprintData footprint);
            bool canBuild;

            if (hasIrregularFootprint)
            {
                canBuild = BuildableRules.CanBuildOffsets(
                    state.FactoryLayer,
                    state.TerrainLayer,
                    command.X,
                    command.Y,
                    footprint.OffsetXs,
                    footprint.OffsetYs,
                    footprint.Length,
                    buildRule,
                    state.TerrainResourceChannelIndex);
            }
            else
            {
                int footprintWidth = command.FootprintWidth > 0 ? command.FootprintWidth : 1;
                int footprintHeight = command.FootprintHeight > 0 ? command.FootprintHeight : 1;

                canBuild = BuildableRules.CanBuildRect(
                    state.FactoryLayer,
                    state.TerrainLayer,
                    command.X,
                    command.Y,
                    footprintWidth,
                    footprintHeight,
                    buildRule,
                    state.TerrainResourceChannelIndex);
            }

            if (!canBuild)
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.NotBuildable;
                return;
            }

            int variantId = GridCellData.SetOrientation(0, command.Orientation);

            if (hasIrregularFootprint)
            {
                for (int i = 0; i < footprint.Length; i++)
                {
                    int x = command.X + footprint.OffsetXs[i];
                    int y = command.Y + footprint.OffsetYs[i];
                    state.FactoryLayer.TrySetCellState(x, y, command.StateId, variantId, 0u, tickIndex, out _);
                }
            }
            else
            {
                int footprintWidth = command.FootprintWidth > 0 ? command.FootprintWidth : 1;
                int footprintHeight = command.FootprintHeight > 0 ? command.FootprintHeight : 1;

                for (int localY = 0; localY < footprintHeight; localY++)
                {
                    int y = command.Y + localY;

                    for (int localX = 0; localX < footprintWidth; localX++)
                    {
                        int x = command.X + localX;
                        state.FactoryLayer.TrySetCellState(x, y, command.StateId, variantId, 0u, tickIndex, out _);
                    }
                }
            }

            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = command.StateId;
        }

        private static void ApplyRemoveCommand(ref MinimalFactoryGameState state, FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!state.FactoryLayer.TrySetCellState(command.X, command.Y, (int)GridStateId.Empty, 0, 0u, tickIndex, out _))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return;
            }

            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = (int)GridStateId.Empty;
        }

        private static void ApplyRotateCommand(ref MinimalFactoryGameState state, FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!state.FactoryLayer.TryGet(command.X, command.Y, out GridCellData existingCell))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return;
            }

            if (existingCell.StateId == (int)GridStateId.Empty)
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.EmptyCell;
                return;
            }

            int nextVariantId = GridCellData.SetOrientation(existingCell.VariantId, command.Orientation);
            state.FactoryLayer.TrySetCellState(command.X, command.Y, existingCell.StateId, nextVariantId, existingCell.Flags, tickIndex, out _);
            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = existingCell.StateId;
        }

        private static void ApplyMoveCommand(ref MinimalFactoryGameState state, FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!state.FactoryLayer.TryGet(command.X, command.Y, out GridCellData sourceCell))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return;
            }

            if (sourceCell.StateId == (int)GridStateId.Empty)
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.EmptyCell;
                return;
            }

            if (command.FootprintId >= 0 || command.FootprintWidth > 1 || command.FootprintHeight > 1)
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.Unsupported;
                return;
            }

            BuildableRuleData buildRule;
            if (!BuildableRules.TryGetRule(state.BuildableRules, sourceCell.StateId, out buildRule))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.MissingBuildRule;
                return;
            }

            if (!BuildableRules.CanBuildSingleCell(
                state.FactoryLayer,
                state.TerrainLayer,
                command.TargetX,
                command.TargetY,
                buildRule,
                state.TerrainResourceChannelIndex))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.NotBuildable;
                return;
            }

            if (!state.FactoryLayer.TrySetCellState(command.X, command.Y, (int)GridStateId.Empty, 0, 0u, tickIndex, out _))
            {
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return;
            }

            if (!state.FactoryLayer.TrySetCellState(command.TargetX, command.TargetY, sourceCell.StateId, sourceCell.VariantId, sourceCell.Flags, tickIndex, out _))
            {
                state.FactoryLayer.TrySetCellState(command.X, command.Y, sourceCell.StateId, sourceCell.VariantId, sourceCell.Flags, tickIndex, out _);
                result.Success = false;
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return;
            }

            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = sourceCell.StateId;
        }

        private static bool TryGetFootprint(FactoryFootprintData[] footprints, int footprintId, out FactoryFootprintData footprint)
        {
            if (footprints == null || footprintId < 0 || footprintId >= footprints.Length)
            {
                footprint = default;
                return false;
            }

            footprint = footprints[footprintId];
            if (footprint.Length <= 0 || footprint.OffsetXs == null || footprint.OffsetYs == null)
            {
                footprint = default;
                return false;
            }

            if (footprint.Length > footprint.OffsetXs.Length || footprint.Length > footprint.OffsetYs.Length)
            {
                footprint = default;
                return false;
            }

            return true;
        }

        private static FactoryCommandResult BuildDefaultResult(int commandIndex, FactoryCommand command)
        {
            return new FactoryCommandResult
            {
                CommandIndex = commandIndex,
                CommandType = command.Type,
                X = command.X,
                Y = command.Y,
                Success = false,
                FailureReason = FactoryCommandFailureReason.UnknownCommand,
                AppliedStateId = (int)GridStateId.Empty,
            };
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
