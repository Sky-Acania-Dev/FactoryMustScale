using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class ConveyorPayloadMovementTests
    {
        [Test]
        public void ConveyorChain_ContinuousMovement_PropagatesWithinSameTick()
        {
            // Layout (all conveyors facing right):
            // [0]=11, [1]=22, [2]=33, [3]=empty
            // Expected after one tick (same-tick propagation):
            // [0]=empty, [1]=11, [2]=22, [3]=33
            var harness = CreateRunningHarness(width: 4, height: 1);

            SetConveyor(harness.State.FactoryLayer, 0, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 1, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 2, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 3, 0, CellOrientation.Right);

            SetPayload(harness.State.FactoryLayer, 0, 0, 11);
            SetPayload(harness.State.FactoryLayer, 1, 0, 22);
            SetPayload(harness.State.FactoryLayer, 2, 0, 33);
            SetPayload(harness.State.FactoryLayer, 3, 0, 0);

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 0, 0, 0);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 11);
            AssertPayload(harness.State.FactoryLayer, 2, 0, 22);
            AssertPayload(harness.State.FactoryLayer, 3, 0, 33);
        }

        [Test]
        public void ConveyorChain_BlockedDeadEnd_StopsEntireChainInSameTick()
        {
            // Layout (all conveyors facing right):
            // [0]=11, [1]=22, [2]=33, [3]=44 (dead-end: last belt points out-of-range)
            // Expected after one tick:
            // no movement at all due to propagated blocking from the terminal belt.
            var harness = CreateRunningHarness(width: 4, height: 1);

            SetConveyor(harness.State.FactoryLayer, 0, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 1, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 2, 0, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 3, 0, CellOrientation.Right);

            SetPayload(harness.State.FactoryLayer, 0, 0, 11);
            SetPayload(harness.State.FactoryLayer, 1, 0, 22);
            SetPayload(harness.State.FactoryLayer, 2, 0, 33);
            SetPayload(harness.State.FactoryLayer, 3, 0, 44);

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 0, 0, 11);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 22);
            AssertPayload(harness.State.FactoryLayer, 2, 0, 33);
            AssertPayload(harness.State.FactoryLayer, 3, 0, 44);
        }

        [Test]
        public void ConveyorConflict_TwoSourcesOneTarget_UsesLowestSourceIndex()
        {
            // Grid indexes on width=3:
            // y=0: [0][1][2]
            // y=1: [3][4][5]
            // source A: index 1 (1,0) Down -> target index 4 (1,1)
            // source B: index 3 (0,1) Right -> target index 4 (1,1)
            // Winner should be index 1 (lower source index).
            var harness = CreateRunningHarness(width: 3, height: 2);

            SetConveyor(harness.State.FactoryLayer, 1, 0, CellOrientation.Down);
            SetConveyor(harness.State.FactoryLayer, 0, 1, CellOrientation.Right);
            SetConveyor(harness.State.FactoryLayer, 1, 1, CellOrientation.Right);

            SetPayload(harness.State.FactoryLayer, 1, 0, 101);
            SetPayload(harness.State.FactoryLayer, 0, 1, 202);
            SetPayload(harness.State.FactoryLayer, 1, 1, 0);

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 1, 0, 0);
            AssertPayload(harness.State.FactoryLayer, 0, 1, 202);
            AssertPayload(harness.State.FactoryLayer, 1, 1, 101);
        }

        private static FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem> CreateRunningHarness(int width, int height)
        {
            var state = new MinimalFactoryGameState
            {
                Seed = 0,
                TerrainResourceChannelIndex = 0,
                FactoryPayloadItemChannelIndex = 0,
                MaxFactoryTicks = 128,
                FactoryTicksExecuted = 0,
                Phase = MinimalFactoryGamePhase.Running,
                TerrainLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                FactoryLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                CommandQueue = new FactoryCommandQueue(capacity: 8),
                CommandResults = new FactoryCommandResultBuffer(capacity: 8),
                BuildableRules = new BuildableRuleData[0],
                Footprints = new FactoryFootprintData[0],
                StopProcessingOnFailure = false,
            };

            return new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                state,
                new MinimalFactoryGameLoopSystem());
        }

        private static void SetConveyor(Layer layer, int x, int y, CellOrientation orientation)
        {
            int variantId = GridCellData.SetOrientation(0, orientation);
            bool placed = layer.TrySetCellState(x, y, (int)GridStateId.Conveyor, variantId, 0u, currentTick: 0, out _);
            Assert.That(placed, Is.True);
        }

        private static void SetPayload(Layer layer, int x, int y, int payload)
        {
            bool set = layer.TrySetPayload(x, y, channelIndex: 0, payloadValue: payload);
            Assert.That(set, Is.True);
        }

        private static void AssertPayload(Layer layer, int x, int y, int expected)
        {
            bool found = layer.TryGetPayload(x, y, channelIndex: 0, out int value);
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }
    }
}
