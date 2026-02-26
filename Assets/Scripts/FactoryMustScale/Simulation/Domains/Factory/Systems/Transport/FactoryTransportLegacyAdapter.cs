namespace FactoryMustScale.Simulation.Domains.Factory.Systems.Transport
{
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Legacy;

    /// <summary>
    /// Deterministic belt-only transport system mapped to the SimLoop 3-phase contract.
    ///
    /// Tick contract:
    /// - PreCompute ingests external commands in stable order and clears transient routing buffers.
    /// - Compute reads authoritative primary buffers only and builds a resolved move plan.
    /// - Commit performs a full two-buffer apply: copy+advance progress, apply resolved moves, then swap.
    ///
    /// The progress rule intentionally clamps at 4 for blocked items: min(progress + 1, 4).
    /// </summary>
    public sealed class FactoryTransportLegacyAdapter : ISimSystem, ISimHashSource
    {
        public const int EmptyItemId = 0;
        public const int InvalidIndex = -1;
        public const int BeltBuildingType = 1;

        public const int CommandPlaceBelt = 1;
        public const int CommandRemoveCell = 2;
        public const int CommandRotateCell = 3;
        public const int CommandInjectItem = 4;

        private FactoryCoreLoopState _state;

        public FactoryTransportLegacyAdapter(in FactoryCoreLoopState initialState)
        {
            _state = initialState;
        }

        public int PreComputeRunCount { get; private set; }

        public int ComputeRunCount { get; private set; }

        public int CommitRunCount { get; private set; }

        public int[] ItemIdByCell => _state.ItemPayloadByCell;

        public int[] ProgressByCell => _state.ItemTransportProgressByCell;

        public int[] DirectionByCell => _state.DirectionByCell;

        public int[] BuildingTypeByCell => _state.BuildingTypeByCell;

        public void PreCompute(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            int cellCount = ResolveCellCount();
            if (cellCount > 0)
            {
                EnsureBuffers(cellCount);
                IngestExternalCommands(cellCount);
                ClearTransientBuffers(cellCount);
                DerivePerCellFlags(cellCount);

                // Early commit injection point: producers can queue item insertions here in the future.
                EarlyCommitInjectFromProducers();
                // Early commit injection point: sinks can queue item removals here in the future.
                EarlyCommitConsumeToSinks();
            }

            PreComputeRunCount++;
        }

        public void Compute(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            int cellCount = ResolveCellCount();
            if (cellCount > 0)
            {
                BuildMoveIntents(cellCount);
                ResolveConflicts(cellCount);
            }

            ComputeRunCount++;
        }

        public void Commit(ref SimContext ctx)
        {
            if (!ctx.Clock.IsFactoryTick)
            {
                return;
            }

            int cellCount = ResolveCellCount();
            if (cellCount > 0)
            {
                InitializeNextBuffers(cellCount);
                ApplyResolvedMoves(cellCount);
                SwapPrimaryAndNextBuffers();
            }

            CommitRunCount++;
        }

        public void AppendHash(ref SimHashBuilder builder)
        {
            AppendArray(ref builder, _state.ItemPayloadByCell, _state.ItemPayloadByCell != null ? _state.ItemPayloadByCell.Length : 0);
            AppendArray(ref builder, _state.ItemTransportProgressByCell, _state.ItemTransportProgressByCell != null ? _state.ItemTransportProgressByCell.Length : 0);
            AppendArray(ref builder, _state.DirectionByCell, _state.DirectionByCell != null ? _state.DirectionByCell.Length : 0);
            AppendArray(ref builder, _state.BuildingTypeByCell, _state.BuildingTypeByCell != null ? _state.BuildingTypeByCell.Length : 0);
        }

        private int ResolveCellCount()
        {
            if (_state.ItemPayloadByCell != null)
            {
                return _state.ItemPayloadByCell.Length;
            }

            if (_state.BuildingTypeByCell != null)
            {
                return _state.BuildingTypeByCell.Length;
            }

            return 0;
        }

        private void EnsureBuffers(int cellCount)
        {
            if (_state.ItemPayloadByCell == null || _state.ItemPayloadByCell.Length != cellCount)
            {
                _state.ItemPayloadByCell = new int[cellCount];
            }

            if (_state.ItemNextPayloadByCell == null || _state.ItemNextPayloadByCell.Length != cellCount)
            {
                _state.ItemNextPayloadByCell = new int[cellCount];
            }

            if (_state.ItemTransportProgressByCell == null || _state.ItemTransportProgressByCell.Length != cellCount)
            {
                _state.ItemTransportProgressByCell = new int[cellCount];
            }

            if (_state.ItemNextTransportProgressByCell == null || _state.ItemNextTransportProgressByCell.Length != cellCount)
            {
                _state.ItemNextTransportProgressByCell = new int[cellCount];
            }

            if (_state.BuildingTypeByCell == null || _state.BuildingTypeByCell.Length != cellCount)
            {
                _state.BuildingTypeByCell = new int[cellCount];
            }

            if (_state.DirectionByCell == null || _state.DirectionByCell.Length != cellCount)
            {
                _state.DirectionByCell = new int[cellCount];
            }

            if (_state.ItemIntentTargetBySource == null || _state.ItemIntentTargetBySource.Length != cellCount)
            {
                _state.ItemIntentTargetBySource = new int[cellCount];
            }

            if (_state.ItemResolvedSourceByTarget == null || _state.ItemResolvedSourceByTarget.Length != cellCount)
            {
                _state.ItemResolvedSourceByTarget = new int[cellCount];
            }

            if (_state.ItemResolvedTargetBySource == null || _state.ItemResolvedTargetBySource.Length != cellCount)
            {
                _state.ItemResolvedTargetBySource = new int[cellCount];
            }

            if (_state.IsBeltByCell == null || _state.IsBeltByCell.Length != cellCount)
            {
                _state.IsBeltByCell = new bool[cellCount];
            }

            if (_state.HasItemByCell == null || _state.HasItemByCell.Length != cellCount)
            {
                _state.HasItemByCell = new bool[cellCount];
            }

            if (_state.CanReceiveByCell == null || _state.CanReceiveByCell.Length != cellCount)
            {
                _state.CanReceiveByCell = new bool[cellCount];
            }

            if (_state.OutputTargetIndexByCell == null || _state.OutputTargetIndexByCell.Length != cellCount)
            {
                _state.OutputTargetIndexByCell = new int[cellCount];
            }
        }

        private void IngestExternalCommands(int cellCount)
        {
            int commandCount = _state.CommandCount;
            if (_state.CommandTypeByIndex == null || _state.CommandCellIndexByIndex == null || _state.CommandDirOrItemIdByIndex == null)
            {
                _state.CommandCount = 0;
                return;
            }

            if (commandCount > _state.CommandTypeByIndex.Length)
            {
                commandCount = _state.CommandTypeByIndex.Length;
            }

            if (commandCount > _state.CommandCellIndexByIndex.Length)
            {
                commandCount = _state.CommandCellIndexByIndex.Length;
            }

            if (commandCount > _state.CommandDirOrItemIdByIndex.Length)
            {
                commandCount = _state.CommandDirOrItemIdByIndex.Length;
            }

            for (int i = 0; i < commandCount; i++)
            {
                int cellIndex = _state.CommandCellIndexByIndex[i];
                if (cellIndex < 0 || cellIndex >= cellCount)
                {
                    continue;
                }

                int arg = _state.CommandDirOrItemIdByIndex[i];
                switch (_state.CommandTypeByIndex[i])
                {
                    case CommandPlaceBelt:
                        _state.BuildingTypeByCell[cellIndex] = BeltBuildingType;
                        _state.DirectionByCell[cellIndex] = arg & 3;
                        break;
                    case CommandRemoveCell:
                        _state.BuildingTypeByCell[cellIndex] = 0;
                        _state.DirectionByCell[cellIndex] = 0;
                        _state.ItemPayloadByCell[cellIndex] = EmptyItemId;
                        _state.ItemTransportProgressByCell[cellIndex] = 0;
                        break;
                    case CommandRotateCell:
                        if (_state.BuildingTypeByCell[cellIndex] == BeltBuildingType)
                        {
                            _state.DirectionByCell[cellIndex] = arg & 3;
                        }

                        break;
                    case CommandInjectItem:
                        if (_state.BuildingTypeByCell[cellIndex] == BeltBuildingType && _state.ItemPayloadByCell[cellIndex] == EmptyItemId)
                        {
                            _state.ItemPayloadByCell[cellIndex] = arg;
                            _state.ItemTransportProgressByCell[cellIndex] = 0;
                        }

                        break;
                }
            }

            _state.CommandCount = 0;
        }

        private void ClearTransientBuffers(int cellCount)
        {
            for (int i = 0; i < cellCount; i++)
            {
                _state.ItemIntentTargetBySource[i] = InvalidIndex;
                _state.ItemResolvedSourceByTarget[i] = InvalidIndex;
                _state.ItemResolvedTargetBySource[i] = InvalidIndex;
                _state.OutputTargetIndexByCell[i] = InvalidIndex;
            }
        }

        private void DerivePerCellFlags(int cellCount)
        {
            for (int i = 0; i < cellCount; i++)
            {
                bool isBelt = _state.BuildingTypeByCell[i] == BeltBuildingType;
                bool hasItem = _state.ItemPayloadByCell[i] != EmptyItemId;
                _state.IsBeltByCell[i] = isBelt;
                _state.HasItemByCell[i] = hasItem;

                int target = ComputeOutputTargetIndex(i, _state.DirectionByCell[i], cellCount);
                _state.OutputTargetIndexByCell[i] = target;

                bool canReceive = false;
                if (target >= 0 && target < cellCount)
                {
                    canReceive = _state.BuildingTypeByCell[target] == BeltBuildingType
                        && _state.ItemPayloadByCell[target] == EmptyItemId;
                }

                _state.CanReceiveByCell[i] = canReceive;
            }
        }

        private int ComputeOutputTargetIndex(int sourceIndex, int direction, int cellCount)
        {
            int width = _state.FactoryLayer != null ? _state.FactoryLayer.Width : cellCount;
            if (width <= 0)
            {
                return InvalidIndex;
            }

            int x = sourceIndex % width;
            int y = sourceIndex / width;

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

            if (x < 0 || x >= width)
            {
                return InvalidIndex;
            }

            int height = cellCount / width;
            if (y < 0 || y >= height)
            {
                return InvalidIndex;
            }

            int target = (y * width) + x;
            return (target < 0 || target >= cellCount) ? InvalidIndex : target;
        }

        private void BuildMoveIntents(int cellCount)
        {
            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                if (!_state.IsBeltByCell[sourceIndex] || !_state.HasItemByCell[sourceIndex])
                {
                    continue;
                }

                if (_state.ItemTransportProgressByCell[sourceIndex] < 4)
                {
                    continue;
                }

                int targetIndex = _state.OutputTargetIndexByCell[sourceIndex];
                if (targetIndex == InvalidIndex)
                {
                    continue;
                }

                if (!_state.CanReceiveByCell[sourceIndex])
                {
                    continue;
                }

                _state.ItemIntentTargetBySource[sourceIndex] = targetIndex;
            }
        }

        private void ResolveConflicts(int cellCount)
        {
            for (int sourceIndex = 0; sourceIndex < cellCount; sourceIndex++)
            {
                int targetIndex = _state.ItemIntentTargetBySource[sourceIndex];
                if (targetIndex == InvalidIndex)
                {
                    continue;
                }

                int currentWinner = _state.ItemResolvedSourceByTarget[targetIndex];
                if (currentWinner == InvalidIndex || sourceIndex < currentWinner)
                {
                    _state.ItemResolvedSourceByTarget[targetIndex] = sourceIndex;
                }
            }

            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int sourceIndex = _state.ItemResolvedSourceByTarget[targetIndex];
                if (sourceIndex != InvalidIndex)
                {
                    _state.ItemResolvedTargetBySource[sourceIndex] = targetIndex;
                }
            }
        }

        private void InitializeNextBuffers(int cellCount)
        {
            for (int i = 0; i < cellCount; i++)
            {
                int itemId = _state.ItemPayloadByCell[i];
                _state.ItemNextPayloadByCell[i] = itemId;

                if (itemId != EmptyItemId)
                {
                    int nextProgress = _state.ItemTransportProgressByCell[i] + 1;
                    _state.ItemNextTransportProgressByCell[i] = nextProgress > 4 ? 4 : nextProgress;
                }
                else
                {
                    _state.ItemNextTransportProgressByCell[i] = 0;
                }
            }
        }

        private void ApplyResolvedMoves(int cellCount)
        {
            for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
            {
                int sourceIndex = _state.ItemResolvedSourceByTarget[targetIndex];
                if (sourceIndex == InvalidIndex)
                {
                    continue;
                }

                int itemId = _state.ItemPayloadByCell[sourceIndex];
                if (itemId == EmptyItemId)
                {
                    continue;
                }

                _state.ItemNextPayloadByCell[sourceIndex] = EmptyItemId;
                _state.ItemNextTransportProgressByCell[sourceIndex] = 0;
                _state.ItemNextPayloadByCell[targetIndex] = itemId;
                _state.ItemNextTransportProgressByCell[targetIndex] = 0;
            }
        }

        private void SwapPrimaryAndNextBuffers()
        {
            int[] payload = _state.ItemPayloadByCell;
            _state.ItemPayloadByCell = _state.ItemNextPayloadByCell;
            _state.ItemNextPayloadByCell = payload;

            int[] progress = _state.ItemTransportProgressByCell;
            _state.ItemTransportProgressByCell = _state.ItemNextTransportProgressByCell;
            _state.ItemNextTransportProgressByCell = progress;
        }

        private static void EarlyCommitInjectFromProducers()
        {
        }

        private static void EarlyCommitConsumeToSinks()
        {
        }

        private static void AppendArray(ref SimHashBuilder builder, int[] values, int activeCount)
        {
            if (values == null || activeCount <= 0)
            {
                builder.AppendInt(0);
                return;
            }

            if (activeCount > values.Length)
            {
                activeCount = values.Length;
            }

            builder.AppendInt(activeCount);
            for (int i = 0; i < activeCount; i++)
            {
                builder.AppendInt(values[i]);
            }
        }
    }
}
