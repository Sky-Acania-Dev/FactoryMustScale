namespace FactoryMustScale.Simulation.Domains.Factory.Systems.Build
{
    using FactoryMustScale.Simulation.Core;

    /// <summary>
    /// Factory structural edit system under the canonical PreCompute/Compute/Commit contract.
    ///
    /// - PreCompute(A): ingest build/remove/rotate commands into structural intent buffer.
    /// - PreCompute(B): resolve deterministic conflicts and apply structural edits early.
    /// - Compute: read-only for GridCellData.
    /// - Commit: reserved for non-structural deltas.
    /// </summary>
    public sealed class FactoryBuildStructuralSystem : ISimSystem
    {
        private FactoryBuildSystemState _state;

        public FactoryBuildStructuralSystem(in FactoryBuildSystemState initialState)
        {
            _state = initialState;
        }

        public FactoryBuildSystemState State => _state;

        public void PreCompute(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            _state.CommandResults.Clear();
            IngestStructuralIntents();
            ApplyStructuralIntents(ctx.Clock.UnitTick);
            _state.CommandQueue.Clear();
        }

        public void Compute(ref SimContext ctx)
        {
        }

        public void Commit(ref SimContext ctx)
        {
        }

        private void IngestStructuralIntents()
        {
            _state.StructuralIntentBuffer.Clear();

            int commandCount = _state.CommandQueue.Count;
            for (int i = 0; i < commandCount; i++)
            {
                FactoryCommand command = _state.CommandQueue.GetAt(i);
                if (command.Type == FactoryCommandType.PlaceBuilding
                    || command.Type == FactoryCommandType.RemoveBuilding
                    || command.Type == FactoryCommandType.RotateBuilding)
                {
                    _state.StructuralIntentBuffer.TryEnqueue(command);
                }
            }
        }

        private void ApplyStructuralIntents(int tickIndex)
        {
            if (_state.FactoryLayer == null)
            {
                return;
            }

            int cellCount = _state.FactoryLayer.Width * _state.FactoryLayer.Height;
            EnsureScratchCapacity(cellCount);

            for (int i = 0; i < cellCount; i++)
            {
                _state.StructuralIntentWinnerByCell[i] = -1;
                _state.StructuralIntentWriteStampByCell[i] = -1;
            }

            int intentCount = _state.StructuralIntentBuffer.Count;
            for (int i = 0; i < intentCount; i++)
            {
                FactoryCommand intent = _state.StructuralIntentBuffer.GetAt(i);
                if (!TryGetCellIndex(intent.X, intent.Y, out int cellIndex))
                {
                    continue;
                }

                _state.StructuralIntentWinnerByCell[cellIndex] = i;
                _state.StructuralIntentWriteStampByCell[cellIndex] = i;
            }

            bool anyMutation = false;
            for (int i = 0; i < intentCount; i++)
            {
                FactoryCommand command = _state.StructuralIntentBuffer.GetAt(i);
                FactoryCommandResult result = BuildDefaultResult(i, command);

                if (!TryGetCellIndex(command.X, command.Y, out int cellIndex))
                {
                    result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                    _state.CommandResults.TryAdd(result);
                    continue;
                }

                if (_state.StructuralIntentWinnerByCell[cellIndex] != i || _state.StructuralIntentWriteStampByCell[cellIndex] != i)
                {
                    result.FailureReason = FactoryCommandFailureReason.Unsupported;
                    _state.CommandResults.TryAdd(result);
                    continue;
                }

                switch (command.Type)
                {
                    case FactoryCommandType.PlaceBuilding:
                        anyMutation |= ApplyPlaceCommand(command, tickIndex, ref result);
                        break;
                    case FactoryCommandType.RemoveBuilding:
                        anyMutation |= ApplyRemoveCommand(command, tickIndex, ref result);
                        break;
                    case FactoryCommandType.RotateBuilding:
                        anyMutation |= ApplyRotateCommand(command, tickIndex, ref result);
                        break;
                    default:
                        result.FailureReason = FactoryCommandFailureReason.UnknownCommand;
                        break;
                }

                _state.CommandResults.TryAdd(result);
                if (!result.Success && _state.StopProcessingOnFailure)
                {
                    break;
                }
            }

            if (anyMutation)
            {
                RefreshActiveSets();
            }
        }

        private bool ApplyPlaceCommand(FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!BuildableRules.TryGetRule(_state.BuildableRules, command.StateId, out BuildableRuleData buildRule))
            {
                result.FailureReason = FactoryCommandFailureReason.MissingBuildRule;
                return false;
            }

            bool hasFootprint = TryGetFootprint(command.FootprintId, out FactoryFootprintData footprint);
            bool canBuild;

            if (hasFootprint)
            {
                canBuild = BuildableRules.CanBuildOffsets(
                    _state.FactoryLayer,
                    _state.TerrainLayer,
                    command.X,
                    command.Y,
                    footprint.OffsetXs,
                    footprint.OffsetYs,
                    footprint.Length,
                    buildRule,
                    _state.TerrainResourceChannelIndex);
            }
            else
            {
                int width = command.FootprintWidth > 0 ? command.FootprintWidth : 1;
                int height = command.FootprintHeight > 0 ? command.FootprintHeight : 1;

                canBuild = BuildableRules.CanBuildRect(
                    _state.FactoryLayer,
                    _state.TerrainLayer,
                    command.X,
                    command.Y,
                    width,
                    height,
                    buildRule,
                    _state.TerrainResourceChannelIndex);
            }

            if (!canBuild)
            {
                result.FailureReason = FactoryCommandFailureReason.NotBuildable;
                return false;
            }

            int variantId = GridCellData.SetOrientation(0, command.Orientation);
            variantId = GridCellData.SetConstructionDestructionStage(variantId, GridCellData.StageFullyBuilt);

            if (hasFootprint)
            {
                for (int i = 0; i < footprint.Length; i++)
                {
                    int x = command.X + footprint.OffsetXs[i];
                    int y = command.Y + footprint.OffsetYs[i];
                    _state.FactoryLayer.TrySetCellState(x, y, command.StateId, variantId, 0u, tickIndex, out _);
                }
            }
            else
            {
                int width = command.FootprintWidth > 0 ? command.FootprintWidth : 1;
                int height = command.FootprintHeight > 0 ? command.FootprintHeight : 1;

                for (int localY = 0; localY < height; localY++)
                {
                    int y = command.Y + localY;
                    for (int localX = 0; localX < width; localX++)
                    {
                        int x = command.X + localX;
                        _state.FactoryLayer.TrySetCellState(x, y, command.StateId, variantId, 0u, tickIndex, out _);
                    }
                }
            }

            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = command.StateId;
            return true;
        }

        private bool ApplyRemoveCommand(FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!_state.FactoryLayer.TrySetCellState(command.X, command.Y, (int)GridStateId.Empty, 0, 0u, tickIndex, out _))
            {
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return false;
            }

            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = (int)GridStateId.Empty;
            return true;
        }

        private bool ApplyRotateCommand(FactoryCommand command, int tickIndex, ref FactoryCommandResult result)
        {
            if (!_state.FactoryLayer.TryGet(command.X, command.Y, out GridCellData existingCell))
            {
                result.FailureReason = FactoryCommandFailureReason.OutOfRange;
                return false;
            }

            if (existingCell.StateId == (int)GridStateId.Empty)
            {
                result.FailureReason = FactoryCommandFailureReason.EmptyCell;
                return false;
            }

            int nextVariantId = GridCellData.SetOrientation(existingCell.VariantId, command.Orientation);
            _state.FactoryLayer.TrySetCellState(command.X, command.Y, existingCell.StateId, nextVariantId, existingCell.Flags, tickIndex, out _);
            result.Success = true;
            result.FailureReason = FactoryCommandFailureReason.None;
            result.AppliedStateId = existingCell.StateId;
            return true;
        }

        private void RefreshActiveSets()
        {
            int cellCount = _state.FactoryLayer.Width * _state.FactoryLayer.Height;

            if (_state.ActiveBeltCellIndices == null || _state.ActiveBeltCellIndices.Length != cellCount)
            {
                _state.ActiveBeltCellIndices = new int[cellCount];
            }

            if (_state.ActiveProcessCellIndices == null || _state.ActiveProcessCellIndices.Length != cellCount)
            {
                _state.ActiveProcessCellIndices = new int[cellCount];
            }

            _state.ActiveBeltCellCount = 0;
            _state.ActiveProcessCellCount = 0;

            int width = _state.FactoryLayer.Width;
            int height = _state.FactoryLayer.Height;

            for (int localY = 0; localY < height; localY++)
            {
                int y = _state.FactoryLayer.MinY + localY;
                int rowStart = localY * width;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = _state.FactoryLayer.MinX + localX;
                    if (!_state.FactoryLayer.TryGet(x, y, out GridCellData cell))
                    {
                        continue;
                    }

                    int index = rowStart + localX;

                    if (cell.StateId == (int)GridStateId.Conveyor)
                    {
                        _state.ActiveBeltCellIndices[_state.ActiveBeltCellCount++] = index;
                    }

                    if (cell.StateId == (int)GridStateId.CrafterCore
                        || cell.StateId == (int)GridStateId.CrafterInputPort
                        || cell.StateId == (int)GridStateId.CrafterOutputPort)
                    {
                        _state.ActiveProcessCellIndices[_state.ActiveProcessCellCount++] = index;
                    }
                }
            }
        }

        private void EnsureScratchCapacity(int cellCount)
        {
            if (_state.StructuralIntentWinnerByCell == null || _state.StructuralIntentWinnerByCell.Length != cellCount)
            {
                _state.StructuralIntentWinnerByCell = new int[cellCount];
            }

            if (_state.StructuralIntentWriteStampByCell == null || _state.StructuralIntentWriteStampByCell.Length != cellCount)
            {
                _state.StructuralIntentWriteStampByCell = new int[cellCount];
            }
        }

        private bool TryGetFootprint(int footprintId, out FactoryFootprintData footprint)
        {
            if (_state.Footprints == null || footprintId < 0 || footprintId >= _state.Footprints.Length)
            {
                footprint = default;
                return false;
            }

            footprint = _state.Footprints[footprintId];
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

        private bool TryGetCellIndex(int x, int y, out int cellIndex)
        {
            if (_state.FactoryLayer == null || !_state.FactoryLayer.IsInRange(x, y))
            {
                cellIndex = -1;
                return false;
            }

            int localX = x - _state.FactoryLayer.MinX;
            int localY = y - _state.FactoryLayer.MinY;
            cellIndex = (localY * _state.FactoryLayer.Width) + localX;
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
    }
}
