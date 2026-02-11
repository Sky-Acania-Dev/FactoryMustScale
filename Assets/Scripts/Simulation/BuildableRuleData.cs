namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Runtime buildability rule data baked from authoring definitions.
    /// </summary>
    public struct BuildableRuleData
    {
        public int StateId;
        public TerrainTypeMask AllowedTerrains;
        public ResourceTypeMask AllowedResources;
    }

    public static class BuildableRules
    {
        public static ResourceTypeMask ToMask(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.None:
                    return ResourceTypeMask.NoneResource;
                case ResourceType.Ore:
                    return ResourceTypeMask.Ore;
                case ResourceType.Liquid:
                    return ResourceTypeMask.Liquid;
                case ResourceType.Geothermal:
                    return ResourceTypeMask.Geothermal;
                case ResourceType.OreTier2:
                    return ResourceTypeMask.OreTier2;
                case ResourceType.OreTier3:
                    return ResourceTypeMask.OreTier3;
                default:
                    return ResourceTypeMask.None;
            }
        }

        public static TerrainTypeMask ToMask(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.None:
                    return TerrainTypeMask.NoneTerrain;
                case TerrainType.Ground:
                    return TerrainTypeMask.Ground;
                case TerrainType.Water:
                    return TerrainTypeMask.Water;
                case TerrainType.Cliff:
                    return TerrainTypeMask.Cliff;
                case TerrainType.Blocked:
                    return TerrainTypeMask.Blocked;
                case TerrainType.ResourceDeposit:
                    return TerrainTypeMask.ResourceDeposit;
                case TerrainType.GeothermalSite:
                    return TerrainTypeMask.GeothermalSite;
                default:
                    return TerrainTypeMask.None;
            }
        }

        public static bool IsBuildableOnTerrain(TerrainType terrainType, TerrainTypeMask allowedTerrains)
        {
            TerrainTypeMask terrainMask = ToMask(terrainType);
            return (allowedTerrains & terrainMask) != TerrainTypeMask.None;
        }

        public static bool IsBuildableOnResource(ResourceType terrainResource, ResourceTypeMask allowedResources)
        {
            ResourceTypeMask terrainMask = ToMask(terrainResource);
            return (allowedResources & terrainMask) != ResourceTypeMask.None;
        }

        public static bool CanBuildSingleCell(
            Layer factoryLayer,
            Layer terrainLayer,
            int x,
            int y,
            BuildableRuleData rule,
            int terrainResourceChannelIndex)
        {
            if (factoryLayer == null || terrainLayer == null)
            {
                return false;
            }

            if (!factoryLayer.TryGet(x, y, out GridCellData factoryCell) || factoryCell.StateId != (int)GridStateId.Empty)
            {
                return false;
            }

            return IsTerrainCellBuildableForRule(terrainLayer, x, y, rule, terrainResourceChannelIndex);
        }

        public static bool CanBuildMultiCell(
            Layer factoryLayer,
            Layer terrainLayer,
            int originX,
            int originY,
            int[] offsetXs,
            int[] offsetYs,
            int footprintLength,
            BuildableRuleData rule,
            int terrainResourceChannelIndex)
        {
            if (factoryLayer == null || terrainLayer == null || offsetXs == null || offsetYs == null)
            {
                return false;
            }

            if (footprintLength <= 0 || footprintLength > offsetXs.Length || footprintLength > offsetYs.Length)
            {
                return false;
            }

            for (int i = 0; i < footprintLength; i++)
            {
                int x = originX + offsetXs[i];
                int y = originY + offsetYs[i];

                if (!factoryLayer.TryGet(x, y, out GridCellData factoryCell) || factoryCell.StateId != (int)GridStateId.Empty)
                {
                    return false;
                }

                if (!IsTerrainCellBuildableForRule(terrainLayer, x, y, rule, terrainResourceChannelIndex))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryGetRule(BuildableRuleData[] rules, int stateId, out BuildableRuleData rule)
        {
            if (rules == null)
            {
                rule = default;
                return false;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].StateId == stateId)
                {
                    rule = rules[i];
                    return true;
                }
            }

            rule = default;
            return false;
        }

        private static bool IsTerrainCellBuildableForRule(
            Layer terrainLayer,
            int x,
            int y,
            BuildableRuleData rule,
            int terrainResourceChannelIndex)
        {
            if (!terrainLayer.TryGet(x, y, out GridCellData terrainCell))
            {
                return false;
            }

            TerrainType terrainType = (TerrainType)terrainCell.StateId;
            if (!IsBuildableOnTerrain(terrainType, rule.AllowedTerrains))
            {
                return false;
            }

            if (!terrainLayer.TryGetPayload(x, y, terrainResourceChannelIndex, out int resourceTypeValue))
            {
                return false;
            }

            ResourceType resourceType = (ResourceType)resourceTypeValue;
            return IsBuildableOnResource(resourceType, rule.AllowedResources);
        }
    }
}
