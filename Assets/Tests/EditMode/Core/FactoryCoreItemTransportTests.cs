using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Item;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class FactoryCoreItemTransportTests
    {
        [Test]
        public void ConveyorArbitratedPropagation_ChainPropagatesWithinSameTick()
        {
            FactoryCoreLoopState initialState = CreateState(width: 4, height: 1, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetConveyor(initialState.FactoryLayer, 0, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 2, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 3, 0, CellOrientation.Right);

            SetPayload(initialState.FactoryLayer, 0, 0, 11);
            SetPayload(initialState.FactoryLayer, 1, 0, 22);
            SetPayload(initialState.FactoryLayer, 2, 0, 33);
            SetPayload(initialState.FactoryLayer, 3, 0, 0);

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 0, 0, 0);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 11);
            AssertPayload(harness.State.FactoryLayer, 2, 0, 22);
            AssertPayload(harness.State.FactoryLayer, 3, 0, 33);
        }

        [Test]
        public void ConveyorArbitratedPropagation_BlockedDeadEndStopsChain()
        {
            FactoryCoreLoopState initialState = CreateState(width: 4, height: 1, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetConveyor(initialState.FactoryLayer, 0, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 2, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 3, 0, CellOrientation.Right);

            SetPayload(initialState.FactoryLayer, 0, 0, 11);
            SetPayload(initialState.FactoryLayer, 1, 0, 22);
            SetPayload(initialState.FactoryLayer, 2, 0, 33);
            SetPayload(initialState.FactoryLayer, 3, 0, 44);

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 0, 0, 11);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 22);
            AssertPayload(harness.State.FactoryLayer, 2, 0, 33);
            AssertPayload(harness.State.FactoryLayer, 3, 0, 44);
        }

        private static FactoryCoreLoopState CreateState(int width, int height, ItemTransportAlgorithm algorithm)
        {
            return new FactoryCoreLoopState
            {
                Running = true,
                MaxFactoryTicks = 8,
                FactoryTicksExecuted = 0,
                FactoryPayloadItemChannelIndex = 0,
                ItemTransportAlgorithm = algorithm,
                FactoryLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                PhaseTraceBuffer = new int[16],
                PhaseTraceCount = 0,
            };
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
