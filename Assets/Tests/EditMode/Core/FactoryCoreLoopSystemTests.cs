using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Build;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class FactoryCoreLoopSystemTests
    {
        [Test]
        public void PreCompute_StructuralCommit_HappensBeforeCompute()
        {
            Layer terrain = new Layer(0, 0, 1, 1, payloadChannelCount: 1);
            Layer factory = new Layer(0, 0, 1, 1, payloadChannelCount: 0);
            terrain.TrySetCellState(0, 0, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
            terrain.TrySetPayload(0, 0, 0, (int)ResourceType.None);

            FactoryBuildSystemState state = CreateState(terrain, factory);
            state.CommandQueue.TryEnqueue(new FactoryCommand
            {
                Type = FactoryCommandType.PlaceBuilding,
                X = 0,
                Y = 0,
                StateId = (int)GridStateId.Conveyor,
                Orientation = (int)CellOrientation.Left,
                FootprintWidth = 1,
                FootprintHeight = 1,
            });

            var buildSystem = new FactoryCoreLoopSystem(in state);
            var probe = new ComputeProbeSystem(factory, 0, 0);
            var loop = new SimLoop(new ISimSystem[] { buildSystem, probe });

            loop.Tick(new SimClock(4));

            Assert.That(probe.ObservedStateIdInCompute, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(probe.ObservedOrientationInCompute, Is.EqualTo(CellOrientation.Left));

            Assert.That(factory.TryGet(0, 0, out GridCellData cell), Is.True);
            Assert.That(cell.StateId, Is.EqualTo((int)GridStateId.Conveyor));
            Assert.That(GridCellData.GetOrientationEnum(cell.VariantId), Is.EqualTo(CellOrientation.Left));
        }

        [Test]
        public void Compute_DoesNotMutateGridCellData_ChangeCountStableWithoutIntents()
        {
            Layer terrain = new Layer(0, 0, 1, 1, payloadChannelCount: 1);
            Layer factory = new Layer(0, 0, 1, 1, payloadChannelCount: 0);
            terrain.TrySetCellState(0, 0, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
            terrain.TrySetPayload(0, 0, 0, (int)ResourceType.None);

            factory.TrySetCellState(0, 0, (int)GridStateId.Conveyor, 0, 0u, currentTick: 0, out GridCellData initialCell);

            FactoryBuildSystemState state = CreateState(terrain, factory);
            var buildSystem = new FactoryCoreLoopSystem(in state);
            var loop = new SimLoop(new ISimSystem[] { buildSystem });

            loop.Tick(new SimClock(4));

            Assert.That(factory.TryGet(0, 0, out GridCellData after), Is.True);
            Assert.That(after.ChangeCount, Is.EqualTo(initialCell.ChangeCount));
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
                CommandQueue = new FactoryCommandQueue(capacity: 8),
                StructuralIntentBuffer = new FactoryCommandQueue(capacity: 8),
                CommandResults = new FactoryCommandResultBuffer(capacity: 8),
            };
        }

        private sealed class ComputeProbeSystem : ISimSystem
        {
            private readonly Layer _factory;
            private readonly int _x;
            private readonly int _y;

            public ComputeProbeSystem(Layer factory, int x, int y)
            {
                _factory = factory;
                _x = x;
                _y = y;
                ObservedOrientationInCompute = CellOrientation.Invalid;
            }

            public int ObservedStateIdInCompute { get; private set; }
            public CellOrientation ObservedOrientationInCompute { get; private set; }

            public void PreCompute(ref SimContext ctx)
            {
            }

            public void Compute(ref SimContext ctx)
            {
                if (_factory.TryGet(_x, _y, out GridCellData cell))
                {
                    ObservedStateIdInCompute = cell.StateId;
                    ObservedOrientationInCompute = GridCellData.GetOrientationEnum(cell.VariantId);
                }
            }

            public void Commit(ref SimContext ctx)
            {
            }
        }
    }
}
