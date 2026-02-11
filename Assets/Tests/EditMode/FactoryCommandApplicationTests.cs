using FactoryMustScale.Simulation;
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
            };

            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                initialState,
                new MinimalFactoryGameLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.FactoryLayer.TryGet(1, 1, out GridCellData placedCell), Is.True);
            Assert.That(placedCell.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(GridCellData.GetOrientation(placedCell.VariantId), Is.EqualTo((int)CellOrientation.Left));
            Assert.That(harness.State.CommandQueue.Count, Is.EqualTo(0));

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
        }
    }
}
