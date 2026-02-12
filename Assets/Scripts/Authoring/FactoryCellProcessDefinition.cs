using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Authoring
{
    [System.Serializable]
    public struct FactoryProcessSlotDefinition
    {
        public CardinalDirectionMask Directions;
        public int ItemIdFilter;
        public int MaxItemsPerTick;
    }

    [System.Serializable]
    public struct ProcessItemStackDefinition
    {
        public int ItemId;
        public int Amount;
    }

    [System.Serializable]
    public struct ProcessResourceDeltaDefinition
    {
        public ProcessResourceType ResourceType;
        public int Amount;
    }

    [System.Serializable]
    public struct FactoryProcessRecipeDefinition
    {
        public ProcessItemStackDefinition[] ItemInputs;
        public ProcessItemStackDefinition[] ItemOutputs;
        public ProcessResourceDeltaDefinition[] ResourceDeltas;
    }

    [CreateAssetMenu(
        fileName = "FactoryCellProcessDefinition",
        menuName = "FactoryMustScale/Definitions/Factory Cell Process")]
    public sealed class FactoryCellProcessDefinition : ScriptableObject
    {
        [SerializeField]
        private GridStateId _stateId = GridStateId.Empty;

        [SerializeField]
        private FactoryProcessType _processType = FactoryProcessType.None;

        [SerializeField, Min(1)]
        private int _processDurationTicks = 1;

        [SerializeField]
        private int _processProgressPayloadChannelIndex = -1;

        [SerializeField]
        private FactoryProcessSlotDefinition[] _inputSlots;

        [SerializeField]
        private FactoryProcessSlotDefinition[] _outputSlots;

        [SerializeField]
        private FactoryProcessRecipeDefinition[] _recipes;

        public GridStateId StateId => _stateId;
        public FactoryProcessType ProcessType => _processType;
        public int ProcessDurationTicks => _processDurationTicks;
        public int ProcessProgressPayloadChannelIndex => _processProgressPayloadChannelIndex;
        public FactoryProcessSlotDefinition[] InputSlots => _inputSlots;
        public FactoryProcessSlotDefinition[] OutputSlots => _outputSlots;
        public FactoryProcessRecipeDefinition[] Recipes => _recipes;

        public FactoryCellProcessData BakeToRuntimeData()
        {
            var data = new FactoryCellProcessData
            {
                StateId = (int)_stateId,
                ProcessType = _processType,
                ProcessDurationTicks = _processDurationTicks > 0 ? _processDurationTicks : 1,
                ProcessProgressPayloadChannelIndex = _processProgressPayloadChannelIndex,
                InputSlots = BakeSlots(_inputSlots),
                OutputSlots = BakeSlots(_outputSlots),
                Recipes = BakeRecipes(_recipes),
            };

            return data;
        }

        private static FactoryProcessSlotData[] BakeSlots(FactoryProcessSlotDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
            {
                return new FactoryProcessSlotData[0];
            }

            var data = new FactoryProcessSlotData[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                data[i].Directions = definitions[i].Directions;
                data[i].ItemIdFilter = definitions[i].ItemIdFilter;
                data[i].MaxItemsPerTick = definitions[i].MaxItemsPerTick;
            }

            return data;
        }

        private static FactoryProcessRecipeData[] BakeRecipes(FactoryProcessRecipeDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
            {
                return new FactoryProcessRecipeData[0];
            }

            var data = new FactoryProcessRecipeData[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                data[i].ItemInputs = BakeItemStacks(
                    definitions[i].ItemInputs,
                    FactoryProcessRecipeData.ItemInputCount);

                data[i].ItemOutputs = BakeItemStacks(
                    definitions[i].ItemOutputs,
                    FactoryProcessRecipeData.ItemOutputCount);

                data[i].ResourceDeltas = BakeResourceDeltas(
                    definitions[i].ResourceDeltas,
                    FactoryProcessRecipeData.ResourceDeltaCount);
            }

            return data;
        }

        private static ProcessItemStackData[] BakeItemStacks(ProcessItemStackDefinition[] definitions, int fixedLength)
        {
            var data = new ProcessItemStackData[fixedLength];
            if (definitions == null || definitions.Length == 0)
            {
                return data;
            }

            int count = definitions.Length < fixedLength ? definitions.Length : fixedLength;
            for (int i = 0; i < count; i++)
            {
                data[i].ItemId = definitions[i].ItemId;
                data[i].Amount = definitions[i].Amount;
            }

            return data;
        }

        private static ProcessResourceDeltaData[] BakeResourceDeltas(ProcessResourceDeltaDefinition[] definitions, int fixedLength)
        {
            var data = new ProcessResourceDeltaData[fixedLength];
            if (definitions == null || definitions.Length == 0)
            {
                return data;
            }

            int count = definitions.Length < fixedLength ? definitions.Length : fixedLength;
            for (int i = 0; i < count; i++)
            {
                data[i].ResourceType = definitions[i].ResourceType;
                data[i].Amount = definitions[i].Amount;
            }

            return data;
        }
    }
}
