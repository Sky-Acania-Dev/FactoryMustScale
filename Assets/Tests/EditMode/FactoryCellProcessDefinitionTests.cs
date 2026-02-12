using System.Reflection;
using FactoryMustScale.Authoring;
using FactoryMustScale.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class FactoryCellProcessDefinitionTests
    {
        [Test]
        public void BakeToRuntimeData_MapsDefinitionFields_AndUsesFixedRecipeDimensions()
        {
            var definition = ScriptableObject.CreateInstance<FactoryCellProcessDefinition>();

            SetPrivateField(definition, "_stateId", GridStateId.CrafterCore);
            SetPrivateField(definition, "_processType", FactoryProcessType.Craft);
            SetPrivateField(definition, "_processDurationTicks", 6);
            SetPrivateField(definition, "_processProgressPayloadChannelIndex", 3);
            SetPrivateField(definition, "_inputSlots", new[]
            {
                new FactoryProcessSlotDefinition
                {
                    Directions = CardinalDirectionMask.Left,
                    ItemIdFilter = 101,
                    MaxItemsPerTick = 2,
                }
            });
            SetPrivateField(definition, "_outputSlots", new[]
            {
                new FactoryProcessSlotDefinition
                {
                    Directions = CardinalDirectionMask.Right,
                    ItemIdFilter = 0,
                    MaxItemsPerTick = 1,
                }
            });
            SetPrivateField(definition, "_recipes", new[]
            {
                new FactoryProcessRecipeDefinition
                {
                    ItemInputs = new[]
                    {
                        new ProcessItemStackDefinition { ItemId = 101, Amount = 2 },
                        new ProcessItemStackDefinition { ItemId = 102, Amount = 1 },
                    },
                    ItemOutputs = new[]
                    {
                        new ProcessItemStackDefinition { ItemId = 201, Amount = 1 },
                    },
                    ResourceDeltas = new[]
                    {
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.ElectricPower, Amount = -5 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.Heat, Amount = 2 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.MechanicalWork, Amount = -1 },
                    },
                }
            });

            FactoryCellProcessData runtime = definition.BakeToRuntimeData();

            Assert.That(runtime.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(runtime.ProcessType, Is.EqualTo(FactoryProcessType.Craft));
            Assert.That(runtime.ProcessDurationTicks, Is.EqualTo(6));
            Assert.That(runtime.ProcessProgressPayloadChannelIndex, Is.EqualTo(3));
            Assert.That(runtime.InputSlots.Length, Is.EqualTo(1));
            Assert.That(runtime.OutputSlots.Length, Is.EqualTo(1));
            Assert.That(runtime.Recipes.Length, Is.EqualTo(1));
            Assert.That(runtime.InputSlots[0].Directions, Is.EqualTo(CardinalDirectionMask.Left));
            Assert.That(runtime.OutputSlots[0].Directions, Is.EqualTo(CardinalDirectionMask.Right));

            Assert.That(runtime.Recipes[0].ItemInputs.Length, Is.EqualTo(FactoryProcessRecipeData.ItemInputCount));
            Assert.That(runtime.Recipes[0].ItemOutputs.Length, Is.EqualTo(FactoryProcessRecipeData.ItemOutputCount));
            Assert.That(runtime.Recipes[0].ResourceDeltas.Length, Is.EqualTo(FactoryProcessRecipeData.ResourceDeltaCount));

            Assert.That(runtime.Recipes[0].ItemInputs[0].ItemId, Is.EqualTo(101));
            Assert.That(runtime.Recipes[0].ItemInputs[1].ItemId, Is.EqualTo(102));
            Assert.That(runtime.Recipes[0].ItemInputs[2].ItemId, Is.EqualTo(0));

            Assert.That(runtime.Recipes[0].ItemOutputs[0].ItemId, Is.EqualTo(201));
            Assert.That(runtime.Recipes[0].ItemOutputs[1].ItemId, Is.EqualTo(0));

            Assert.That(runtime.Recipes[0].ResourceDeltas[0].ResourceType, Is.EqualTo(ProcessResourceType.ElectricPower));
            Assert.That(runtime.Recipes[0].ResourceDeltas[1].ResourceType, Is.EqualTo(ProcessResourceType.Heat));
            Assert.That(runtime.Recipes[0].ResourceDeltas[2].ResourceType, Is.EqualTo(ProcessResourceType.MechanicalWork));
            Assert.That(runtime.Recipes[0].ResourceDeltas[3].ResourceType, Is.EqualTo(ProcessResourceType.None));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void BakeToRuntimeData_TruncatesExcessRecipeEntriesToFixedDimensions()
        {
            var definition = ScriptableObject.CreateInstance<FactoryCellProcessDefinition>();

            SetPrivateField(definition, "_recipes", new[]
            {
                new FactoryProcessRecipeDefinition
                {
                    ItemInputs = new[]
                    {
                        new ProcessItemStackDefinition { ItemId = 1, Amount = 1 },
                        new ProcessItemStackDefinition { ItemId = 2, Amount = 1 },
                        new ProcessItemStackDefinition { ItemId = 3, Amount = 1 },
                        new ProcessItemStackDefinition { ItemId = 4, Amount = 1 },
                    },
                    ItemOutputs = new[]
                    {
                        new ProcessItemStackDefinition { ItemId = 10, Amount = 1 },
                        new ProcessItemStackDefinition { ItemId = 11, Amount = 1 },
                        new ProcessItemStackDefinition { ItemId = 12, Amount = 1 },
                    },
                    ResourceDeltas = new[]
                    {
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.ElectricPower, Amount = 1 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.Heat, Amount = 1 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.MechanicalWork, Amount = 1 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.Reserved, Amount = 1 },
                        new ProcessResourceDeltaDefinition { ResourceType = ProcessResourceType.ElectricPower, Amount = 99 },
                    },
                }
            });

            FactoryCellProcessData runtime = definition.BakeToRuntimeData();

            Assert.That(runtime.Recipes[0].ItemInputs.Length, Is.EqualTo(3));
            Assert.That(runtime.Recipes[0].ItemInputs[2].ItemId, Is.EqualTo(3));

            Assert.That(runtime.Recipes[0].ItemOutputs.Length, Is.EqualTo(2));
            Assert.That(runtime.Recipes[0].ItemOutputs[1].ItemId, Is.EqualTo(11));

            Assert.That(runtime.Recipes[0].ResourceDeltas.Length, Is.EqualTo(4));
            Assert.That(runtime.Recipes[0].ResourceDeltas[3].ResourceType, Is.EqualTo(ProcessResourceType.Reserved));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void GridCellData_ProcessProfileId_RoundTripsInVariantCodeBits()
        {
            int variantId = 0;
            variantId = GridCellData.SetProcessProfileId(variantId, processProfileId: 55);

            Assert.That(GridCellData.GetProcessProfileId(variantId), Is.EqualTo(55));

            variantId = GridCellData.SetOrientation(variantId, CellOrientation.Left);
            Assert.That(GridCellData.GetOrientationEnum(variantId), Is.EqualTo(CellOrientation.Left));
            Assert.That(GridCellData.GetProcessProfileId(variantId), Is.EqualTo(55));
        }

        [Test]
        public void FactoryProcessExamples_BasicConveyorPreset_IsValidPassThroughDefinition()
        {
            FactoryCellProcessData data = FactoryProcessExamples.CreateBasicConveyorProcessData();

            Assert.That(data.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(data.ProcessType, Is.EqualTo(FactoryProcessType.PassThrough));
            Assert.That(data.ProcessDurationTicks, Is.EqualTo(1));
            Assert.That(data.ProcessProgressPayloadChannelIndex, Is.EqualTo(-1));
            Assert.That(data.InputSlots.Length, Is.EqualTo(1));
            Assert.That(data.OutputSlots.Length, Is.EqualTo(1));
            Assert.That(data.Recipes.Length, Is.EqualTo(0));

            Assert.That(data.InputSlots[0].Directions, Is.EqualTo(CardinalDirectionMask.All));
            Assert.That(data.OutputSlots[0].Directions, Is.EqualTo(CardinalDirectionMask.All));
            Assert.That(data.InputSlots[0].MaxItemsPerTick, Is.EqualTo(1));
            Assert.That(data.OutputSlots[0].MaxItemsPerTick, Is.EqualTo(1));
        }

        [Test]
        public void BakeToRuntimeData_BasicConveyorAuthoring_UsesFixedRecipeDimensions()
        {
            var definition = ScriptableObject.CreateInstance<FactoryCellProcessDefinition>();

            SetPrivateField(definition, "_stateId", GridStateId.Conveyor);
            SetPrivateField(definition, "_processType", FactoryProcessType.PassThrough);
            SetPrivateField(definition, "_processDurationTicks", 1);
            SetPrivateField(definition, "_processProgressPayloadChannelIndex", -1);
            SetPrivateField(definition, "_inputSlots", new[]
            {
                new FactoryProcessSlotDefinition
                {
                    Directions = CardinalDirectionMask.All,
                    ItemIdFilter = 0,
                    MaxItemsPerTick = 1,
                }
            });
            SetPrivateField(definition, "_outputSlots", new[]
            {
                new FactoryProcessSlotDefinition
                {
                    Directions = CardinalDirectionMask.All,
                    ItemIdFilter = 0,
                    MaxItemsPerTick = 1,
                }
            });
            SetPrivateField(definition, "_recipes", new[]
            {
                new FactoryProcessRecipeDefinition
                {
                    ItemInputs = new ProcessItemStackDefinition[0],
                    ItemOutputs = new[] { new ProcessItemStackDefinition { ItemId = 7001, Amount = 1 } },
                    ResourceDeltas = new[]
                    {
                        new ProcessResourceDeltaDefinition
                        {
                            ResourceType = ProcessResourceType.Reserved,
                            Amount = 0,
                        }
                    },
                }
            });

            FactoryCellProcessData runtime = definition.BakeToRuntimeData();

            Assert.That(runtime.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(runtime.ProcessType, Is.EqualTo(FactoryProcessType.PassThrough));
            Assert.That(runtime.Recipes.Length, Is.EqualTo(1));
            Assert.That(runtime.Recipes[0].ItemOutputs.Length, Is.EqualTo(2));
            Assert.That(runtime.Recipes[0].ResourceDeltas.Length, Is.EqualTo(4));
            Assert.That(runtime.Recipes[0].ItemOutputs[0].ItemId, Is.EqualTo(7001));
            Assert.That(runtime.Recipes[0].ItemOutputs[1].ItemId, Is.EqualTo(0));

            Object.DestroyImmediate(definition);
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
