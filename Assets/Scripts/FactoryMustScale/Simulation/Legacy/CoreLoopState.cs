namespace FactoryMustScale.Simulation.Legacy
{
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Item;

    /// <summary>
    /// Backwards-compatible state for systems still typed against the previous legacy name.
    /// </summary>
    public struct FactoryCoreLoopState
    {
        public Layer FactoryLayer;

        public ItemTransportAlgorithm ItemTransportAlgorithm;
        public int ItemTransportProgressThreshold;

        public int[] ItemPayloadByCell;
        public int[] ItemNextPayloadByCell;
        public int[] ItemTransportProgressByCell;
        public int[] ItemNextTransportProgressByCell;

        public int[] BuildingTypeByCell;
        public int[] DirectionByCell;
        public bool[] IsBeltByCell;
        public bool[] HasItemByCell;
        public int[] OutputTargetIndexByCell;
        public bool[] CanReceiveByCell;

        public int[] ItemIntentTargetBySource;
        public int[] ItemResolvedSourceByTarget;
        public int[] ItemResolvedTargetBySource;

        public int[] ItemWinnerSourceByTarget;
        public int[] ItemWinnerCountByTarget;
        public int[] ItemMergerRoundRobinCursorByCell;

        public int[] ItemMoveEventSourceByIndex;
        public int[] ItemMoveEventTargetByIndex;
        public int ItemMoveEventCount;

        public int CommandCount;
        public int[] CommandTypeByIndex;
        public int[] CommandCellIndexByIndex;
        public int[] CommandDirOrItemIdByIndex;

        public int FactoryPayloadItemChannelIndex;
        public int[] StorageItemCountByCell;
        public int FactoryTicksExecuted;

        public SimEventBuffer SimEvents;

        public static implicit operator FactoryCoreLoopState(CoreLoopState state)
        {
            return new FactoryCoreLoopState
            {
                FactoryLayer = state.FactoryLayer,
                ItemTransportAlgorithm = state.ItemTransportAlgorithm,
                ItemTransportProgressThreshold = state.ItemTransportProgressThreshold,
                ItemPayloadByCell = state.ItemPayloadByCell,
                ItemNextPayloadByCell = state.ItemNextPayloadByCell,
                ItemTransportProgressByCell = state.ItemTransportProgressByCell,
                ItemNextTransportProgressByCell = state.ItemNextTransportProgressByCell,
                BuildingTypeByCell = state.BuildingTypeByCell,
                DirectionByCell = state.DirectionByCell,
                IsBeltByCell = state.IsBeltByCell,
                HasItemByCell = state.HasItemByCell,
                OutputTargetIndexByCell = state.OutputTargetIndexByCell,
                CanReceiveByCell = state.CanReceiveByCell,
                ItemIntentTargetBySource = state.ItemIntentTargetBySource,
                ItemResolvedSourceByTarget = state.ItemResolvedSourceByTarget,
                ItemResolvedTargetBySource = state.ItemResolvedTargetBySource,
                ItemWinnerSourceByTarget = state.ItemWinnerSourceByTarget,
                ItemWinnerCountByTarget = state.ItemWinnerCountByTarget,
                ItemMergerRoundRobinCursorByCell = state.ItemMergerRoundRobinCursorByCell,
                ItemMoveEventSourceByIndex = state.ItemMoveEventSourceByIndex,
                ItemMoveEventTargetByIndex = state.ItemMoveEventTargetByIndex,
                ItemMoveEventCount = state.ItemMoveEventCount,
                CommandCount = state.CommandCount,
                CommandTypeByIndex = state.CommandTypeByIndex,
                CommandCellIndexByIndex = state.CommandCellIndexByIndex,
                CommandDirOrItemIdByIndex = state.CommandDirOrItemIdByIndex,
                FactoryPayloadItemChannelIndex = state.FactoryPayloadItemChannelIndex,
                StorageItemCountByCell = state.StorageItemCountByCell,
                FactoryTicksExecuted = state.FactoryTicksExecuted,
                SimEvents = state.SimEvents,
            };
        }
    }
}
