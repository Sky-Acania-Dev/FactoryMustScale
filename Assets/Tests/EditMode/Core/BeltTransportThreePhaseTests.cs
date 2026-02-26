using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.ItemTransport;
using FactoryMustScale.Simulation.Legacy;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class BeltTransportThreePhaseTests
    {
        [Test]
        public void Throughput_FiveBelts_MovesOneCellEveryFourFactoryTicks()
        {
            FactoryCoreLoopState state = CreateFiveBeltLineState();

            Assert.That(FindItemIndex(state.ItemPayloadByCell), Is.EqualTo(0));

            for (int tick = 1; tick <= 20; tick++)
            {
                BeltTransportSystem.PreCompute(ref state);
                BeltTransportSystem.Compute(ref state);
                BeltTransportSystem.Commit(ref state);

                int expectedIndex = tick / 4;
                if (expectedIndex > 4)
                {
                    expectedIndex = 4;
                }

                Assert.That(FindItemIndex(state.ItemPayloadByCell), Is.EqualTo(expectedIndex), $"tick={tick}");
            }
        }

        [Test]
        public void Determinism_SameInitialState_ProducesSamePerTickHashes()
        {
            const int ticks = 20;
            ulong[] first = RunScenarioHashes(ticks);
            ulong[] second = RunScenarioHashes(ticks);

            Assert.That(second.Length, Is.EqualTo(first.Length));
            for (int i = 0; i < ticks; i++)
            {
                Assert.That(second[i], Is.EqualTo(first[i]), $"tick={i}");
            }
        }

        private static ulong[] RunScenarioHashes(int ticks)
        {
            FactoryCoreLoopState state = CreateFiveBeltLineState();
            ulong[] hashes = new ulong[ticks];

            for (int tick = 0; tick < ticks; tick++)
            {
                BeltTransportSystem.PreCompute(ref state);
                BeltTransportSystem.Compute(ref state);
                BeltTransportSystem.Commit(ref state);
                hashes[tick] = ComputeStateHash(state.ItemPayloadByCell, state.ItemTransportProgressByCell);
            }

            return hashes;
        }

        private static FactoryCoreLoopState CreateFiveBeltLineState()
        {
            Layer layer = new Layer(0, 0, width: 5, height: 1, payloadChannelCount: 0);
            for (int x = 0; x < 5; x++)
            {
                int variant = GridCellData.SetOrientation(0, CellOrientation.Right);
                layer.TrySetCellState(x, 0, (int)GridStateId.Conveyor, variant, 0u, currentTick: 0, out _);
            }

            return new FactoryCoreLoopState
            {
                FactoryLayer = layer,
                ItemPayloadByCell = new[] { 7, 0, 0, 0, 0 },
                ItemTransportProgressByCell = new int[5],
            };
        }

        private static int FindItemIndex(int[] payload)
        {
            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static ulong ComputeStateHash(int[] payload, int[] progress)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;
            hash = HashArray(payload, hash, prime);
            hash = HashArray(progress, hash, prime);
            return hash;
        }

        private static ulong HashArray(int[] values, ulong seed, ulong prime)
        {
            ulong hash = seed;
            for (int i = 0; i < values.Length; i++)
            {
                unchecked
                {
                    hash ^= (uint)values[i];
                    hash *= prime;
                }
            }

            return hash;
        }
    }
}
