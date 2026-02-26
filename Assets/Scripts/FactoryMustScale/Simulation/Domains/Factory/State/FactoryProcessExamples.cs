namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Canonical process presets used by tests and bootstrap code.
    /// These helpers return fully baked runtime data and do not depend on authoring assets.
    /// </summary>
    public static class FactoryProcessExamples
    {
        public static FactoryCellProcessData CreateBasicConveyorProcessData()
        {
            return new FactoryCellProcessData
            {
                StateId = (int)GridStateId.Conveyor,
                ProcessType = FactoryProcessType.PassThrough,
                ProcessDurationTicks = 1,
                ProcessProgressPayloadChannelIndex = -1,
                InputSlots = new[]
                {
                    new FactoryProcessSlotData
                    {
                        Directions = CardinalDirectionMask.Up,
                        ItemIdFilter = 0,
                        MaxItemsPerTick = 1,
                    },
                },
                OutputSlots = new[]
                {
                    new FactoryProcessSlotData
                    {
                        Directions = CardinalDirectionMask.Down,
                        ItemIdFilter = 0,
                        MaxItemsPerTick = 1,
                    },
                },
                Recipes = new FactoryProcessRecipeData[0],
            };
        }
    }
}
