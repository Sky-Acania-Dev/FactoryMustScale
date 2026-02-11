using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Authoring
{
    [System.Serializable]
    public struct BuildableRuleDefinition
    {
        public GridStateId StateId;
        public TerrainTypeMask AllowedTerrains;
        public ResourceTypeMask AllowedResources;
    }

    [CreateAssetMenu(
        fileName = "BuildableRuleTableDefinition",
        menuName = "FactoryMustScale/Definitions/Buildable Rule Table")]
    public sealed class BuildableRuleTableDefinition : ScriptableObject
    {
        [SerializeField]
        private BuildableRuleDefinition[] _rules;

        public BuildableRuleDefinition[] Rules => _rules;

        public BuildableRuleData[] BakeToRuntimeData()
        {
            if (_rules == null || _rules.Length == 0)
            {
                return new BuildableRuleData[0];
            }

            var runtimeRules = new BuildableRuleData[_rules.Length];
            for (int i = 0; i < _rules.Length; i++)
            {
                runtimeRules[i].StateId = (int)_rules[i].StateId;
                runtimeRules[i].AllowedTerrains = _rules[i].AllowedTerrains;
                runtimeRules[i].AllowedResources = _rules[i].AllowedResources;
            }

            return runtimeRules;
        }
    }
}
