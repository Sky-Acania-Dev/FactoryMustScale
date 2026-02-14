using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Legacy;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class MinimalFactoryGameLoopTests
    {
        private const int TerrainResourceChannel = 0;

        [Test]
        public void GameLoop_InitializesTerrainAndFactory_ThenEndsAfterConfiguredTicks()
        {
            const int width = 12;
            const int height = 8;
            const int seed = 12345;
            const int maxTicks = 5;

            var initialState = CreateInitialState(width, height, seed, maxTicks);
            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                initialState,
                new MinimalFactoryGameLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.Phase, Is.EqualTo(MinimalFactoryGamePhase.Running));
            Assert.That(harness.State.FactoryTicksExecuted, Is.EqualTo(1));

            int resourceDepositCount = 0;
            int geothermalCount = 0;
            int oreResourceCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool terrainFound = harness.State.TerrainLayer.TryGet(x, y, out GridCellData terrainCell);
                    bool factoryFound = harness.State.FactoryLayer.TryGet(x, y, out GridCellData factoryCell);
                    bool payloadFound = harness.State.TerrainLayer.TryGetPayload(x, y, TerrainResourceChannel, out int resourceValue);

                    Assert.That(terrainFound, Is.True);
                    Assert.That(factoryFound, Is.True);
                    Assert.That(payloadFound, Is.True);
                    Assert.That(factoryCell.StateId, Is.EqualTo((int)GridStateId.Empty));

                    var terrainType = (TerrainType)terrainCell.StateId;
                    var resourceType = (ResourceType)resourceValue;

                    if (terrainType == TerrainType.ResourceDeposit)
                    {
                        resourceDepositCount++;
                        Assert.That(resourceType, Is.EqualTo(ResourceType.Ore));
                        oreResourceCount++;
                    }
                    else if (terrainType == TerrainType.GeothermalSite)
                    {
                        geothermalCount++;
                        Assert.That(resourceType, Is.EqualTo(ResourceType.Geothermal));
                    }
                    else
                    {
                        Assert.That(resourceType, Is.EqualTo(ResourceType.None));
                    }
                }
            }

            Assert.That(resourceDepositCount, Is.GreaterThan(0));
            Assert.That(geothermalCount, Is.GreaterThan(0));
            Assert.That(oreResourceCount, Is.EqualTo(resourceDepositCount));

            harness.Tick(maxTicks - 1);

            Assert.That(harness.State.Phase, Is.EqualTo(MinimalFactoryGamePhase.Ended));
            Assert.That(harness.State.FactoryTicksExecuted, Is.EqualTo(maxTicks));
        }

        [Test]
        public void PlacementValidation_WorksForSingleAndMultiCellWithinGameLoopFramework()
        {
            const int width = 14;
            const int height = 14;
            const int seed = 20240619;

            var initialState = CreateInitialState(width, height, seed, maxTicks: 16);
            var harness = new FixedStepSimulationHarness<MinimalFactoryGameState, MinimalFactoryGameLoopSystem>(
                initialState,
                new MinimalFactoryGameLoopSystem());

            harness.Tick(1);

            Layer terrainLayer = harness.State.TerrainLayer;
            Layer factoryLayer = harness.State.FactoryLayer;

            bool foundGround = false;
            bool foundDeposit = false;
            bool foundGroundBlock = false;
            int groundX = 0;
            int groundY = 0;
            int depositX = 0;
            int depositY = 0;
            int blockOriginX = 0;
            int blockOriginY = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    terrainLayer.TryGet(x, y, out GridCellData terrainCell);
                    var terrainType = (TerrainType)terrainCell.StateId;

                    if (!foundGround && terrainType == TerrainType.Ground)
                    {
                        foundGround = true;
                        groundX = x;
                        groundY = y;
                    }

                    if (!foundDeposit && terrainType == TerrainType.ResourceDeposit)
                    {
                        foundDeposit = true;
                        depositX = x;
                        depositY = y;
                    }

                    if (!foundGroundBlock && x < width - 1 && y < height - 1)
                    {
                        if (IsTerrain(terrainLayer, x, y, TerrainType.Ground)
                            && IsTerrain(terrainLayer, x + 1, y, TerrainType.Ground)
                            && IsTerrain(terrainLayer, x, y + 1, TerrainType.Ground)
                            && IsTerrain(terrainLayer, x + 1, y + 1, TerrainType.Ground))
                        {
                            foundGroundBlock = true;
                            blockOriginX = x;
                            blockOriginY = y;
                        }
                    }
                }
            }

            Assert.That(foundGround, Is.True);
            Assert.That(foundDeposit, Is.True);
            Assert.That(foundGroundBlock, Is.True);

            BuildableRuleData conveyorRule = new BuildableRuleData
            {
                StateId = (int)GridStateId.Conveyor,
                AllowedTerrains = TerrainTypeMask.Ground,
                AllowedResources = ResourceTypeMask.NoneResource
            };

            BuildableRuleData minerRule = new BuildableRuleData
            {
                StateId = (int)GridStateId.Conveyor,
                AllowedTerrains = TerrainTypeMask.ResourceDeposit,
                AllowedResources = ResourceTypeMask.Ore
            };

            BuildableRuleData multiCellFactoryRule = new BuildableRuleData
            {
                StateId = (int)GridStateId.CrafterCore,
                AllowedTerrains = TerrainTypeMask.Ground,
                AllowedResources = ResourceTypeMask.NoneResource
            };

            Assert.That(
                BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, groundX, groundY, conveyorRule, TerrainResourceChannel),
                Is.True);

            Assert.That(
                BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, groundX, groundY, minerRule, TerrainResourceChannel),
                Is.False);

            Assert.That(
                BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, depositX, depositY, minerRule, TerrainResourceChannel),
                Is.True);

            factoryLayer.TrySetCellState(groundX, groundY, (int)GridStateId.Conveyor, 0, 0u, currentTick: 1, out _);

            Assert.That(
                BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, groundX, groundY, conveyorRule, TerrainResourceChannel),
                Is.False);

            int[] offsetXs = { 0, 1, 0, 1 };
            int[] offsetYs = { 0, 0, 1, 1 };

            Assert.That(
                BuildableRules.CanBuildMultiCell(
                    factoryLayer,
                    terrainLayer,
                    blockOriginX,
                    blockOriginY,
                    offsetXs,
                    offsetYs,
                    footprintLength: 4,
                    multiCellFactoryRule,
                    TerrainResourceChannel),
                Is.True);

            factoryLayer.TrySetCellState(blockOriginX + 1, blockOriginY + 1, (int)GridStateId.Storage, 0, 0u, currentTick: 1, out _);

            Assert.That(
                BuildableRules.CanBuildMultiCell(
                    factoryLayer,
                    terrainLayer,
                    blockOriginX,
                    blockOriginY,
                    offsetXs,
                    offsetYs,
                    footprintLength: 4,
                    multiCellFactoryRule,
                    TerrainResourceChannel),
                Is.False);
        }

        private static MinimalFactoryGameState CreateInitialState(int width, int height, int seed, int maxTicks)
        {
            return new MinimalFactoryGameState
            {
                Seed = seed,
                TerrainResourceChannelIndex = TerrainResourceChannel,
                MaxFactoryTicks = maxTicks,
                FactoryTicksExecuted = 0,
                Phase = MinimalFactoryGamePhase.PendingInitialization,
                TerrainLayer = new Layer(0, 0, width, height, payloadChannelCount: 1),
                FactoryLayer = new Layer(0, 0, width, height),
            };
        }

        private static bool IsTerrain(Layer terrainLayer, int x, int y, TerrainType expected)
        {
            if (!terrainLayer.TryGet(x, y, out GridCellData terrainCell))
            {
                return false;
            }

            return terrainCell.StateId == (int)expected;
        }
    }
}
