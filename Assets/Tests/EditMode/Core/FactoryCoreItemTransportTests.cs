using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Item;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class FactoryCoreItemTransportTests
    {
        [Test]
        public void ConveyorArbitratedPropagation_MoveIsPublishedThenAppliedNextTick()
        {
            FactoryCoreLoopState initialState = CreateState(width: 3, height: 1, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetConveyor(initialState.FactoryLayer, 0, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 2, 0, CellOrientation.Right);

            SetPayload(initialState.FactoryLayer, 0, 0, 11);
            SetPayload(initialState.FactoryLayer, 1, 0, 0);
            SetPayload(initialState.FactoryLayer, 2, 0, 0);

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.ItemMoveEventCount, Is.EqualTo(1));
            AssertPayload(harness.State.FactoryLayer, 0, 0, 11);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 0);

            harness.Tick(1);

            AssertPayload(harness.State.FactoryLayer, 0, 0, 0);
            AssertPayload(harness.State.FactoryLayer, 1, 0, 11);
        }

        [Test]
        public void ConveyorArbitratedPropagation_NonMergerConflictUsesFirstComeFirstServe()
        {
            FactoryCoreLoopState initialState = CreateState(width: 3, height: 3, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Down);
            SetConveyor(initialState.FactoryLayer, 0, 1, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 1, 1, CellOrientation.Right);

            SetPayload(initialState.FactoryLayer, 1, 0, 10);
            SetPayload(initialState.FactoryLayer, 0, 1, 20);
            SetPayload(initialState.FactoryLayer, 1, 1, 0);

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.ItemMoveEventCount, Is.EqualTo(1));
            Assert.That(harness.State.ItemMoveEventSourceByIndex[0], Is.EqualTo(1)); // y=0,x=1 => index 1 (lower source index wins)
        }

        [Test]
        public void ConveyorArbitratedPropagation_MergerConflictUsesRoundRobinAcceptance()
        {
            FactoryCoreLoopState initialState = CreateState(width: 3, height: 3, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Down);
            SetConveyor(initialState.FactoryLayer, 0, 1, CellOrientation.Right);
            SetMerger(initialState.FactoryLayer, 1, 1, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 2, 1, CellOrientation.Right);

            SetPayload(initialState.FactoryLayer, 1, 0, 10);
            SetPayload(initialState.FactoryLayer, 0, 1, 20);
            SetPayload(initialState.FactoryLayer, 1, 1, 0);
            SetPayload(initialState.FactoryLayer, 2, 1, 0);

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);
            Assert.That(harness.State.ItemMoveEventCount, Is.EqualTo(1));
            Assert.That(harness.State.ItemMoveEventSourceByIndex[0], Is.EqualTo(1));

            // Reset payloads for another merger arbitration round while preserving the merger cursor state.
            FactoryCoreLoopState secondRoundState = harness.State;
            SetPayload(secondRoundState.FactoryLayer, 1, 0, 10);
            SetPayload(secondRoundState.FactoryLayer, 0, 1, 20);
            SetPayload(secondRoundState.FactoryLayer, 1, 1, 0);
            SetPayload(secondRoundState.FactoryLayer, 2, 1, 0);
            secondRoundState.ItemMoveEventCount = 0;

            var secondRoundHarness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                secondRoundState,
                new FactoryCoreLoopSystem());

            secondRoundHarness.Tick(1);
            Assert.That(secondRoundHarness.State.ItemMoveEventCount, Is.EqualTo(1));
            Assert.That(secondRoundHarness.State.ItemMoveEventSourceByIndex[0], Is.EqualTo(3));
        }

        private static FactoryCoreLoopState CreateState(int width, int height, ItemTransportAlgorithm algorithm)
        {
            return new FactoryCoreLoopState
            {
                Running = true,
                MaxFactoryTicks = 16,
                FactoryTicksExecuted = 0,
                FactoryPayloadItemChannelIndex = 0,
                ItemTransportAlgorithm = algorithm,
                ItemTransportProgressThreshold = 1,
                FactoryLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                PhaseTraceBuffer = new int[64],
                PhaseTraceCount = 0,
            };
        }

        private static void SetConveyor(Layer layer, int x, int y, CellOrientation orientation)
        {
            int variantId = GridCellData.SetOrientation(0, orientation);
            bool placed = layer.TrySetCellState(x, y, (int)GridStateId.Conveyor, variantId, 0u, currentTick: 0, out _);
            Assert.That(placed, Is.True);
        }

        private static void SetMerger(Layer layer, int x, int y, CellOrientation orientation)
        {
            int variantId = GridCellData.SetOrientation(0, orientation);
            bool placed = layer.TrySetCellState(x, y, (int)GridStateId.Merger, variantId, 0u, currentTick: 0, out _);
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
