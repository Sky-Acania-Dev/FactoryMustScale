namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Small curated examples for authoring/runtime process presets.
    /// These helpers are intended for tests and bootstrap wiring.
    /// </summary>
    public static class FactoryProcessExamples
    {
        public static FactoryCellProcessData CreateBasicConveyorProcessData()
        {
            FactoryCellProcessData data = new FactoryCellProcessData
            {
                StateId = (int)GridStateId.Conveyor,
                ProcessType = FactoryProcessType.PassThrough,
                ProcessDurationTicks = 1,
                ProcessProgressPayloadChannelIndex = -1,
                InputSlots = new FactoryProcessSlotData[1],
                OutputSlots = new FactoryProcessSlotData[1],
                Recipes = new FactoryProcessRecipeData[0],
            };

            data.InputSlots[0] = new FactoryProcessSlotData
            {
                Directions = CardinalDirectionMask.All,
                ItemIdFilter = 0,
                MaxItemsPerTick = 1,
            };

            data.OutputSlots[0] = new FactoryProcessSlotData
            {
                Directions = CardinalDirectionMask.All,
                ItemIdFilter = 0,
                MaxItemsPerTick = 1,
            };

            return data;
        }
    }
}
