using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Legacy;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class FactoryCommandApplicationTests
    {
        [Test]
        public void CommandQueue_AppliesPlaceRotateRemove_InDeterministicOrder()
        {
            Layer terrainLayer = new Layer(0, 0, 4, 4, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 4, 4);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    factoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Conveyor,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource,
                },
            };

            var commandQueue = new FactoryCommandQueue(capacity: 8);
            Assert.That(commandQueue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 1,
                Y = 1,
                StateId = (int)GridStateId.Conveyor,
                Orientation = (int)CellOrientation.Up,
            }), Is.True);
            Assert.That(commandQueue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.RotateBuilding,
                X = 1,
                Y = 1,
                Orientation = (int)CellOrientation.Left,
            }), Is.True);

            var initialState = new MinimalFactoryGameState
            {
                Seed = 0,
                TerrainResourceChannelIndex = 0,
                MaxFactoryTicks = 10,
                FactoryTicksExecuted = 0,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = terrainLayer,
                FactoryLayer = factoryLayer,
                BuildableRules = rules,
                CommandQueue = commandQueue,
                CommandResults = new FactoryCommandResultBuffer(capacity: 8),
                StopProcessingOnFailure = false,
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                initialState,
                new MinimalFactoryGameLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.FactoryLayer.TryGet(1, 1, out GridCellData placedCell), Is.True);
            Assert.That(placedCell.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(GridCellData.GetOrientation(placedCell.VariantId), Is.EqualTo((int)CellOrientation.Left));
            Assert.That(harness.State.CommandQueue.Count, Is.EqualTo(0));

            Assert.That(harness.State.CommandResults.Count, Is.EqualTo(2));
            FactoryCommandResult placeResult = harness.State.CommandResults.GetAt(0);
            FactoryCommandResult rotateResult = harness.State.CommandResults.GetAt(1);
            Assert.That(placeResult.Success, Is.True);
            Assert.That(rotateResult.Success, Is.True);

            var secondQueue = harness.State.CommandQueue;
            Assert.That(secondQueue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.RemoveBuilding,
                X = 1,
                Y = 1,
            }), Is.True);

            var secondState = harness.State;
            secondState.CommandQueue = secondQueue;

            var secondHarness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                secondState,
                new MinimalFactoryGameLoopSystem());

            secondHarness.Tick(1);

            Assert.That(secondHarness.State.FactoryLayer.TryGet(1, 1, out GridCellData removedCell), Is.True);
            Assert.That(removedCell.StateId, Is.EqualTo((int)GridStateId.Empty));
            Assert.That(secondHarness.State.CommandResults.Count, Is.EqualTo(1));
            Assert.That(secondHarness.State.CommandResults.GetAt(0).Success, Is.True);
        }

        [Test]
        public void CommandResults_CaptureFailures_AndCanStopOnFirstFailure()
        {
            Layer terrainLayer = new Layer(0, 0, 3, 3, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 3, 3);

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    factoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Conveyor,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource,
                },
            };

            var queue = new FactoryCommandQueue(capacity: 8);
            Assert.That(queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 0,
                Y = 0,
                StateId = (int)GridStateId.PowerGenerator,
                Orientation = (int)CellOrientation.Up,
            }), Is.True);
            Assert.That(queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 1,
                Y = 0,
                StateId = (int)GridStateId.Conveyor,
                Orientation = (int)CellOrientation.Right,
            }), Is.True);

            var state = new MinimalFactoryGameState
            {
                Seed = 0,
                TerrainResourceChannelIndex = 0,
                MaxFactoryTicks = 10,
                FactoryTicksExecuted = 0,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = terrainLayer,
                FactoryLayer = factoryLayer,
                BuildableRules = rules,
                CommandQueue = queue,
                CommandResults = new FactoryCommandResultBuffer(capacity: 8),
                StopProcessingOnFailure = true,
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                state,
                new MinimalFactoryGameLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.CommandResults.Count, Is.EqualTo(1));
            FactoryCommandResult failedResult = harness.State.CommandResults.GetAt(0);
            Assert.That(failedResult.Success, Is.False);
            Assert.That(failedResult.FailureReason, Is.EqualTo(FactoryCommandFailureReason.MissingBuildRule));

            Assert.That(harness.State.FactoryLayer.TryGet(1, 0, out GridCellData notPlaced), Is.True);
            Assert.That(notPlaced.StateId, Is.EqualTo((int)GridStateId.Empty));
        }

        [Test]
        public void PlaceBuilding_CanApplyRectangularMultiCellFootprint()
        {
            Layer terrainLayer = new Layer(0, 0, 5, 5, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 5, 5);

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    factoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.CrafterCore,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource,
                },
            };

            var queue = new FactoryCommandQueue(capacity: 4);
            Assert.That(queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 1,
                Y = 1,
                StateId = (int)GridStateId.CrafterCore,
                Orientation = (int)CellOrientation.Up,
                FootprintWidth = 2,
                FootprintHeight = 2,
            }), Is.True);

            var state = new MinimalFactoryGameState
            {
                TerrainResourceChannelIndex = 0,
                MaxFactoryTicks = 4,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = terrainLayer,
                FactoryLayer = factoryLayer,
                BuildableRules = rules,
                CommandQueue = queue,
                CommandResults = new FactoryCommandResultBuffer(capacity: 4),
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(state, new MinimalFactoryGameLoopSystem());
            harness.Tick(1);

            Assert.That(harness.State.CommandResults.Count, Is.EqualTo(1));
            Assert.That(harness.State.CommandResults.GetAt(0).Success, Is.True);

            Assert.That(harness.State.FactoryLayer.TryGet(1, 1, out GridCellData c00), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 1, out GridCellData c10), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(1, 2, out GridCellData c01), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 2, out GridCellData c11), Is.True);

            Assert.That(c00.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(c10.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(c01.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(c11.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
        }


        [Test]
        public void PlaceBuilding_CanApplyIrregularFootprintOffsets()
        {
            Layer terrainLayer = new Layer(0, 0, 6, 6, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 6, 6);

            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    factoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.CrafterCore,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource,
                },
            };

            FactoryFootprintData[] footprints =
            {
                new FactoryFootprintData
                {
                    OffsetXs = new[] { 0, 1, 2, 0, 0 },
                    OffsetYs = new[] { 0, 0, 0, 1, 2 },
                    Length = 5,
                },
            };

            var queue = new FactoryCommandQueue(capacity: 2);
            Assert.That(queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 2,
                Y = 2,
                StateId = (int)GridStateId.CrafterCore,
                Orientation = (int)CellOrientation.Up,
                FootprintId = 0,
            }), Is.True);

            var state = new MinimalFactoryGameState
            {
                TerrainResourceChannelIndex = 0,
                MaxFactoryTicks = 4,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = terrainLayer,
                FactoryLayer = factoryLayer,
                BuildableRules = rules,
                Footprints = footprints,
                CommandQueue = queue,
                CommandResults = new FactoryCommandResultBuffer(capacity: 2),
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(state, new MinimalFactoryGameLoopSystem());
            harness.Tick(1);

            Assert.That(harness.State.CommandResults.GetAt(0).Success, Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 2, out GridCellData p0), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(3, 2, out GridCellData p1), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(4, 2, out GridCellData p2), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 3, out GridCellData p3), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 4, out GridCellData p4), Is.True);

            Assert.That(p0.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(p1.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(p2.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(p3.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(p4.StateId, Is.EqualTo((int)GridStateId.CrafterCore));
            Assert.That(harness.State.FactoryLayer.TryGet(3, 3, out GridCellData untouched), Is.True);
            Assert.That(untouched.StateId, Is.EqualTo((int)GridStateId.Empty));
        }

        [Test]
        public void MoveBuilding_MovesSingleCellBuilding_WhenTargetValid()
        {
            Layer terrainLayer = new Layer(0, 0, 4, 4, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 4, 4);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    factoryLayer.TrySetCellState(x, y, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
                }
            }

            int variantId = GridCellData.SetOrientation(0, (int)CellOrientation.Right);
            factoryLayer.TrySetCellState(1, 1, (int)GridStateId.Conveyor, variantId, 0u, currentTick: 0, out _);

            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Conveyor,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource,
                },
            };

            var queue = new FactoryCommandQueue(capacity: 2);
            Assert.That(queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.MoveBuilding,
                X = 1,
                Y = 1,
                TargetX = 2,
                TargetY = 1,
            }), Is.True);

            var state = new MinimalFactoryGameState
            {
                TerrainResourceChannelIndex = 0,
                MaxFactoryTicks = 4,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = terrainLayer,
                FactoryLayer = factoryLayer,
                BuildableRules = rules,
                CommandQueue = queue,
                CommandResults = new FactoryCommandResultBuffer(capacity: 2),
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(state, new MinimalFactoryGameLoopSystem());
            harness.Tick(1);

            Assert.That(harness.State.CommandResults.GetAt(0).FailureReason, Is.EqualTo(FactoryCommandFailureReason.None));
            Assert.That(harness.State.CommandResults.GetAt(0).Success, Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(1, 1, out GridCellData oldCell), Is.True);
            Assert.That(harness.State.FactoryLayer.TryGet(2, 1, out GridCellData movedCell), Is.True);
            Assert.That(oldCell.StateId, Is.EqualTo((int)GridStateId.Empty));
            Assert.That(movedCell.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(GridCellData.GetOrientation(movedCell.VariantId), Is.EqualTo((int)CellOrientation.Right));
        }
    }
}
