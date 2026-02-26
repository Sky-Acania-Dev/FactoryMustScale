using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Transport;
using FactoryMustScale.Simulation.Legacy;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class BeltTransportThreePhaseTests
    {
        [Test]
        public void Throughput_FiveBelts_MovesOneCellEveryFourFactoryTicks()
        {
            FactoryCoreLoopState initialState = CreateFiveBeltLineState();
            var adapter = new FactoryTransportLegacyAdapter(in initialState);
            var loop = new SimLoop(new ISimSystem[] { adapter });

            // Tick 0
            Assert.That(FindItemIndex(adapter.ItemIdByCell), Is.EqualTo(0));

            for (int tick = 1; tick <= 20; tick++)
            {
                loop.Tick(new SimClock(tick * 4));

                int expectedIndex = tick / 4;
                if (expectedIndex > 4)
                {
                    expectedIndex = 4;
                }

                Assert.That(FindItemIndex(adapter.ItemIdByCell), Is.EqualTo(expectedIndex), $"tick={tick}");
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
            FactoryCoreLoopState initialState = CreateFiveBeltLineState();
            var adapter = new FactoryTransportLegacyAdapter(in initialState);
            var loop = new SimLoop(new ISimSystem[] { adapter });
            ulong[] hashes = new ulong[ticks];

            for (int tick = 0; tick < ticks; tick++)
            {
                loop.Tick(new SimClock((tick + 1) * 4));
                hashes[tick] = ComputeStateHash(adapter.ItemIdByCell, adapter.ProgressByCell, adapter.DirectionByCell, adapter.BuildingTypeByCell);
            }

            return hashes;
        }

        private static FactoryCoreLoopState CreateFiveBeltLineState()
        {
            int[] commandType = new int[1];
            int[] commandCellIndex = new int[1];
            int[] commandArg = new int[1];
            commandType[0] = FactoryTransportLegacyAdapter.CommandInjectItem;
            commandCellIndex[0] = 0;
            commandArg[0] = 7;

            return new FactoryCoreLoopState
            {
                BuildingTypeByCell = new[]
                {
                    FactoryTransportLegacyAdapter.BeltBuildingType,
                    FactoryTransportLegacyAdapter.BeltBuildingType,
                    FactoryTransportLegacyAdapter.BeltBuildingType,
                    FactoryTransportLegacyAdapter.BeltBuildingType,
                    FactoryTransportLegacyAdapter.BeltBuildingType,
                },
                DirectionByCell = new[] { 1, 1, 1, 1, 1 },
                ItemPayloadByCell = new int[5],
                ItemTransportProgressByCell = new int[5],
                CommandTypeByIndex = commandType,
                CommandCellIndexByIndex = commandCellIndex,
                CommandDirOrItemIdByIndex = commandArg,
                CommandCount = 1,
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

        private static ulong ComputeStateHash(int[] payload, int[] progress, int[] direction, int[] buildingType)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;
            hash = HashArray(payload, hash, prime);
            hash = HashArray(progress, hash, prime);
            hash = HashArray(direction, hash, prime);
            hash = HashArray(buildingType, hash, prime);
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
