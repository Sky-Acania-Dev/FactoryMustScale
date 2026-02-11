using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class BuildableRulesTests
    {
        [Test]
        public void ToMask_MapsKnownResourceTypes()
        {
            Assert.That(BuildableRules.ToMask(ResourceType.None), Is.EqualTo(ResourceTypeMask.NoneResource));
            Assert.That(BuildableRules.ToMask(ResourceType.Ore), Is.EqualTo(ResourceTypeMask.Ore));
            Assert.That(BuildableRules.ToMask(ResourceType.Liquid), Is.EqualTo(ResourceTypeMask.Liquid));
            Assert.That(BuildableRules.ToMask(ResourceType.Geothermal), Is.EqualTo(ResourceTypeMask.Geothermal));
        }

        [Test]
        public void ToMask_MapsKnownTerrainTypes()
        {
            Assert.That(BuildableRules.ToMask(TerrainType.None), Is.EqualTo(TerrainTypeMask.NoneTerrain));
            Assert.That(BuildableRules.ToMask(TerrainType.Ground), Is.EqualTo(TerrainTypeMask.Ground));
            Assert.That(BuildableRules.ToMask(TerrainType.OrePatch), Is.EqualTo(TerrainTypeMask.OrePatch));
            Assert.That(BuildableRules.ToMask(TerrainType.GeothermalVent), Is.EqualTo(TerrainTypeMask.GeothermalVent));
        }

        [Test]
        public void IsBuildableOnResource_UsesAllowedMask()
        {
            ResourceTypeMask minerMask = ResourceTypeMask.Ore | ResourceTypeMask.OreTier2 | ResourceTypeMask.OreTier3;

            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.Ore, minerMask), Is.True);
            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.None, minerMask), Is.False);
            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.Liquid, minerMask), Is.False);
        }

        [Test]
        public void IsBuildableOnTerrain_UsesAllowedMask()
        {
            TerrainTypeMask machineTerrainMask = TerrainTypeMask.Ground | TerrainTypeMask.OrePatch;

            Assert.That(BuildableRules.IsBuildableOnTerrain(TerrainType.Ground, machineTerrainMask), Is.True);
            Assert.That(BuildableRules.IsBuildableOnTerrain(TerrainType.OrePatch, machineTerrainMask), Is.True);
            Assert.That(BuildableRules.IsBuildableOnTerrain(TerrainType.Water, machineTerrainMask), Is.False);
        }

        [Test]
        public void CanBuildSingleCell_RequiresEmptyFactoryAndCompatibleTerrainAndResource()
        {
            Layer factoryLayer = new Layer(0, 0, 4, 4);
            Layer terrainLayer = new Layer(0, 0, 4, 4, payloadChannelCount: 1);

            bool terrainSet = terrainLayer.TrySetCellState(1, 1, (int)TerrainType.OrePatch, 0, 0u, currentTick: 0, out _);
            bool resourceSet = terrainLayer.TrySetPayload(1, 1, channelIndex: 0, payloadValue: (int)ResourceType.Ore);
            Assert.That(terrainSet, Is.True);
            Assert.That(resourceSet, Is.True);

            BuildableRuleData minerRule = new BuildableRuleData
            {
                StateId = (int)GridStateId.Conveyor,
                AllowedTerrains = TerrainTypeMask.OrePatch,
                AllowedResources = ResourceTypeMask.Ore
            };

            bool canBuild = BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, 1, 1, minerRule, terrainResourceChannelIndex: 0);
            Assert.That(canBuild, Is.True);

            bool factoryFilled = factoryLayer.TrySetCellState(1, 1, (int)GridStateId.Conveyor, 0, 0u, currentTick: 0, out _);
            Assert.That(factoryFilled, Is.True);

            canBuild = BuildableRules.CanBuildSingleCell(factoryLayer, terrainLayer, 1, 1, minerRule, terrainResourceChannelIndex: 0);
            Assert.That(canBuild, Is.False);
        }

        [Test]
        public void CanBuildMultiCell_FailsFastWhenAnyCellInvalid()
        {
            Layer factoryLayer = new Layer(0, 0, 4, 4);
            Layer terrainLayer = new Layer(0, 0, 4, 4, payloadChannelCount: 1);

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    bool terrainSet = terrainLayer.TrySetCellState(x, y, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
                    bool resourceSet = terrainLayer.TrySetPayload(x, y, 0, (int)ResourceType.None);
                    Assert.That(terrainSet, Is.True);
                    Assert.That(resourceSet, Is.True);
                }
            }

            BuildableRuleData rule = new BuildableRuleData
            {
                StateId = (int)GridStateId.CrafterCore,
                AllowedTerrains = TerrainTypeMask.Ground,
                AllowedResources = ResourceTypeMask.NoneResource
            };

            int[] offsetXs = { 0, 1, 0, 1 };
            int[] offsetYs = { 0, 0, 1, 1 };

            bool canBuild = BuildableRules.CanBuildMultiCell(
                factoryLayer,
                terrainLayer,
                originX: 0,
                originY: 0,
                offsetXs,
                offsetYs,
                footprintLength: 4,
                rule,
                terrainResourceChannelIndex: 0);

            Assert.That(canBuild, Is.True);

            bool blockOneCell = terrainLayer.TrySetCellState(1, 1, (int)TerrainType.Water, 0, 0u, currentTick: 0, out _);
            Assert.That(blockOneCell, Is.True);

            canBuild = BuildableRules.CanBuildMultiCell(
                factoryLayer,
                terrainLayer,
                originX: 0,
                originY: 0,
                offsetXs,
                offsetYs,
                footprintLength: 4,
                rule,
                terrainResourceChannelIndex: 0);

            Assert.That(canBuild, Is.False);
        }

        [Test]
        public void TryGetRule_FindsExpectedRuleByStateId()
        {
            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Conveyor,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource
                },
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Storage,
                    AllowedTerrains = TerrainTypeMask.Ground | TerrainTypeMask.OrePatch,
                    AllowedResources = ResourceTypeMask.NoneResource | ResourceTypeMask.Ore
                },
            };

            bool found = BuildableRules.TryGetRule(rules, (int)GridStateId.Storage, out BuildableRuleData rule);

            Assert.That(found, Is.True);
            Assert.That(rule.StateId, Is.EqualTo((int)GridStateId.Storage));
            Assert.That(rule.AllowedTerrains, Is.EqualTo(TerrainTypeMask.Ground | TerrainTypeMask.OrePatch));
            Assert.That(rule.AllowedResources, Is.EqualTo(ResourceTypeMask.NoneResource | ResourceTypeMask.Ore));
        }

        [Test]
        public void TryGetRule_ReturnsFalse_WhenMissingOrNull()
        {
            BuildableRuleData[] rules =
            {
                new BuildableRuleData
                {
                    StateId = (int)GridStateId.Conveyor,
                    AllowedTerrains = TerrainTypeMask.Ground,
                    AllowedResources = ResourceTypeMask.NoneResource
                },
            };

            Assert.That(BuildableRules.TryGetRule(rules, (int)GridStateId.PowerPole, out BuildableRuleData missingRule), Is.False);
            Assert.That(missingRule.StateId, Is.EqualTo(0));

            Assert.That(BuildableRules.TryGetRule(null, (int)GridStateId.PowerPole, out BuildableRuleData nullRule), Is.False);
            Assert.That(nullRule.StateId, Is.EqualTo(0));
        }
    }
}
