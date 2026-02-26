namespace FactoryMustScale.Simulation.Core
{
    using FactoryMustScale.Simulation.Item;

    /// <summary>
    /// Canonical simulation state shared by transport systems while core-loop migration is in progress.
    /// </summary>
    public struct CoreLoopState
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
    }
}
