using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Item;
using NUnit.Framework;
using System.Text;

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
            Assert.That(harness.State.ItemMoveEventSourceByIndex[0], Is.EqualTo(1));
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

        [Test]
        public void GeneratorToStorage_ProducesDeterministicUnifiedEventStream()
        {
            FactoryCoreLoopState initialState = CreateState(width: 3, height: 1, ItemTransportAlgorithm.ConveyorArbitratedPropagationV1);

            SetCell(initialState.FactoryLayer, 0, 0, GridStateId.Miner, CellOrientation.Right);
            SetConveyor(initialState.FactoryLayer, 1, 0, CellOrientation.Right);
            SetCell(initialState.FactoryLayer, 2, 0, GridStateId.Storage, CellOrientation.Right);

            var firstHarness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());
            var secondHarness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            firstHarness.Tick(4);
            secondHarness.Tick(4);

            Assert.That(firstHarness.State.SimEvents.HistoryCount, Is.GreaterThan(0));
            Assert.That(firstHarness.State.SimEvents.HistoryCount, Is.EqualTo(secondHarness.State.SimEvents.HistoryCount));

            const int expectedHistoryCount = 7;
            string unifiedEventDump = BuildUnifiedEventDump(firstHarness.State.SimEvents);
            // Contract note: over four ticks, miner->conveyor->storage emits
            // generated, transported(apply), generated, transported(apply), stored, transported(apply), generated.
            Assert.That(
                firstHarness.State.SimEvents.HistoryCount,
                Is.EqualTo(expectedHistoryCount),
                $"Unified event history mismatch.\nExpected: {expectedHistoryCount}\nActual: {firstHarness.State.SimEvents.HistoryCount}\nEvents:\n{unifiedEventDump}");

            for (int index = 0; index < firstHarness.State.SimEvents.HistoryCount; index++)
            {
                Assert.That(firstHarness.State.SimEvents.TryGetHistoryEvent(index, out SimEvent firstEvent), Is.True);
                Assert.That(secondHarness.State.SimEvents.TryGetHistoryEvent(index, out SimEvent secondEvent), Is.True);
                Assert.That(firstEvent.Id, Is.EqualTo(secondEvent.Id));
                Assert.That(firstEvent.SourceIndex, Is.EqualTo(secondEvent.SourceIndex));
                Assert.That(firstEvent.TargetIndex, Is.EqualTo(secondEvent.TargetIndex));
                Assert.That(firstEvent.ItemType, Is.EqualTo(secondEvent.ItemType));
            }

            Assert.That(firstHarness.State.SimEvents.TryGetHistoryEvent(0, out SimEvent firstHistoryEvent), Is.True);
            Assert.That(firstHistoryEvent.Id, Is.EqualTo(SimEventId.ItemGenerated));
            Assert.That(firstHarness.State.SimEvents.TryGetHistoryEvent(1, out SimEvent secondHistoryEvent), Is.True);
            Assert.That(secondHistoryEvent.Id, Is.EqualTo(SimEventId.ItemTransported));
            Assert.That(firstHarness.State.SimEvents.TryGetHistoryEvent(3, out SimEvent fourthHistoryEvent), Is.True);
            Assert.That(fourthHistoryEvent.Id, Is.EqualTo(SimEventId.ItemStored));

            Assert.That(firstHarness.State.StorageItemCountByCell[2], Is.GreaterThan(0));
            AssertPayload(firstHarness.State.FactoryLayer, 2, 0, 0);
        }

        private static FactoryCoreLoopState CreateState(int width, int height, ItemTransportAlgorithm algorithm)
        {
            return new FactoryCoreLoopState
            {
                Running = true,
                MaxFactoryTicks = 32,
                FactoryTicksExecuted = 0,
                FactoryPayloadItemChannelIndex = 0,
                ItemTransportAlgorithm = algorithm,
                ItemTransportProgressThreshold = 1,
                SimEventCapacity = 128,
                FactoryLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                PhaseTraceBuffer = new int[64],
                PhaseTraceCount = 0,
            };
        }

        private static void SetCell(Layer layer, int x, int y, GridStateId stateId, CellOrientation orientation)
        {
            int variantId = GridCellData.SetOrientation(0, orientation);
            bool placed = layer.TrySetCellState(x, y, (int)stateId, variantId, 0u, currentTick: 0, out _);
            Assert.That(placed, Is.True);
        }

        private static void SetConveyor(Layer layer, int x, int y, CellOrientation orientation)
        {
            SetCell(layer, x, y, GridStateId.Conveyor, orientation);
        }

        private static void SetMerger(Layer layer, int x, int y, CellOrientation orientation)
        {
            SetCell(layer, x, y, GridStateId.Merger, orientation);
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

        private static string BuildUnifiedEventDump(SimEventBuffer simEvents)
        {
            if (simEvents.HistoryCount == 0)
            {
                return "<no events>";
            }

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < simEvents.HistoryCount; index++)
            {
                if (!simEvents.TryGetHistoryEvent(index, out SimEvent simEvent))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(index)
                    .Append(": id=").Append(simEvent.Id)
                    .Append(", tick=").Append(simEvent.Tick)
                    .Append(", src=").Append(simEvent.SourceKind).Append('#').Append(simEvent.SourceIndex)
                    .Append(", dst=").Append(simEvent.TargetKind).Append('#').Append(simEvent.TargetIndex)
                    .Append(", item=").Append(simEvent.ItemType)
                    .Append(", count=").Append(simEvent.ItemCount);
            }

            return builder.ToString();
        }
    }
}
