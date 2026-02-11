namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Runtime buildability rule data baked from authoring definitions.
    /// </summary>
    public struct BuildableRuleData
    {
        public int StateId;
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

        public static bool IsBuildableOnResource(ResourceType terrainResource, ResourceTypeMask allowedResources)
        {
            ResourceTypeMask terrainMask = ToMask(terrainResource);
            return (allowedResources & terrainMask) != ResourceTypeMask.None;
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
    }
}
