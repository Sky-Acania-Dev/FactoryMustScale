using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class SimLoopHashDeterminismTests
    {
        [SetUp]
        public void SetUp()
        {
            SimLoop.EnableHashChecks = false;
        }

        [TearDown]
        public void TearDown()
        {
            SimLoop.EnableHashChecks = false;
        }

        [Test]
        public void DeterministicReplay_ProducesIdenticalHashSequence()
        {
            const int tickCount = 8;

            ulong[] firstRun = RunScenario(withStructuralDifference: false, tickCount: tickCount);
            ulong[] secondRun = RunScenario(withStructuralDifference: false, tickCount: tickCount);

            Assert.That(firstRun.Length, Is.EqualTo(tickCount));
            Assert.That(secondRun.Length, Is.EqualTo(tickCount));
            for (int i = 0; i < tickCount; i++)
            {
                Assert.That(secondRun[i], Is.EqualTo(firstRun[i]));
            }
        }

        [Test]
        public void DivergenceDetection_StructuralDifferenceChangesHash()
        {
            const int tickCount = 8;

            ulong[] baseline = RunScenario(withStructuralDifference: false, tickCount: tickCount);
            ulong[] divergent = RunScenario(withStructuralDifference: true, tickCount: tickCount);

            bool anyDifference = false;
            for (int i = 0; i < tickCount; i++)
            {
                if (baseline[i] != divergent[i])
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.That(anyDifference, Is.True);
        }

        [Test]
        public void HashDisabledByDefault_DoesNotRecordHashes()
        {
            Layer terrain = new Layer(0, 0, 2, 2, payloadChannelCount: 1);
            Layer factory = new Layer(0, 0, 2, 2, payloadChannelCount: 2);
            InitializeGround(terrain, 2, 2);

            FactoryBuildSystemState state = CreateState(terrain, factory);
            var system = new FactoryCoreLoopSystem(in state);
            var loop = new SimLoop(new ISimSystem[] { system });

            loop.Tick(new SimClock(4));
            loop.Tick(new SimClock(8));

            Assert.That(SimLoop.EnableHashChecks, Is.False);
            Assert.That(loop.HashRecordCount, Is.EqualTo(0));
        }

        private static ulong[] RunScenario(bool withStructuralDifference, int tickCount)
        {
            SimLoop.EnableHashChecks = true;

            Layer terrain = new Layer(0, 0, 2, 2, payloadChannelCount: 1);
            Layer factory = new Layer(0, 0, 2, 2, payloadChannelCount: 2);
            InitializeGround(terrain, 2, 2);

            FactoryBuildSystemState state = CreateState(terrain, factory);
            var system = new FactoryCoreLoopSystem(in state);
            var loop = new SimLoop(new ISimSystem[] { system });

            EnqueueCommonCommands(state.CommandQueue);
            if (withStructuralDifference)
            {
                state.CommandQueue.TryEnqueue(new FactoryCommand
                {
                    Type = FactoryCommandType.PlaceBuilding,
                    X = 1,
                    Y = 1,
                    StateId = (int)GridStateId.Conveyor,
                    Orientation = (int)CellOrientation.Left,
                    FootprintWidth = 1,
                    FootprintHeight = 1,
                });
            }

            for (int i = 0; i < tickCount; i++)
            {
                loop.Tick(new SimClock((i + 1) * 4));
            }

            ulong[] hashes = new ulong[loop.HashRecordCount];
            for (int i = 0; i < loop.HashRecordCount; i++)
            {
                bool found = loop.TryGetHashRecordAt(i, out _, out ulong hashValue);
                Assert.That(found, Is.True);
                hashes[i] = hashValue;
            }

            return hashes;
        }

        private static void EnqueueCommonCommands(FactoryCommandQueue queue)
        {
            queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 0,
                Y = 0,
                StateId = (int)GridStateId.Conveyor,
                Orientation = (int)CellOrientation.Up,
                FootprintWidth = 1,
                FootprintHeight = 1,
            });

            queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 1,
                Y = 0,
                StateId = (int)GridStateId.Conveyor,
                Orientation = (int)CellOrientation.Right,
                FootprintWidth = 1,
                FootprintHeight = 1,
            });

            queue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.RotateBuilding,
                X = 1,
                Y = 0,
                Orientation = (int)CellOrientation.Down,
            });
        }

        private static void InitializeGround(Layer terrain, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    terrain.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    terrain.TrySetPayload(x, y, 0, (int)ResourceType.None);
                }
            }
        }

        private static FactoryBuildSystemState CreateState(Layer terrain, Layer factory)
        {
            return new FactoryBuildSystemState
            {
                TerrainLayer = terrain,
                FactoryLayer = factory,
                TerrainResourceChannelIndex = 0,
                BuildableRules = new[]
                {
                    new BuildableRuleData
                    {
                        StateId = (int)GridStateId.Conveyor,
                        AllowedTerrains = TerrainTypeMask.Ground,
                        AllowedResources = ResourceTypeMask.NoneResource,
                    }
                },
                CommandQueue = new FactoryCommandQueue(capacity: 16),
                StructuralIntentBuffer = new FactoryCommandQueue(capacity: 16),
                CommandResults = new FactoryCommandResultBuffer(capacity: 16),
            };
        }
    }
}
