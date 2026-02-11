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
        public void IsBuildableOnResource_UsesAllowedMask()
        {
            ResourceTypeMask minerMask = ResourceTypeMask.Ore | ResourceTypeMask.OreTier2 | ResourceTypeMask.OreTier3;

            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.Ore, minerMask), Is.True);
            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.None, minerMask), Is.False);
            Assert.That(BuildableRules.IsBuildableOnResource(ResourceType.Liquid, minerMask), Is.False);
        }

        [Test]
        public void TryGetRule_FindsExpectedRuleByStateId()
        {
            BuildableRuleData[] rules =
            {
                new BuildableRuleData { StateId = (int)GridStateId.Conveyor, AllowedResources = ResourceTypeMask.NoneResource },
                new BuildableRuleData { StateId = (int)GridStateId.Storage, AllowedResources = ResourceTypeMask.NoneResource | ResourceTypeMask.Ore },
            };

            bool found = BuildableRules.TryGetRule(rules, (int)GridStateId.Storage, out BuildableRuleData rule);

            Assert.That(found, Is.True);
            Assert.That(rule.StateId, Is.EqualTo((int)GridStateId.Storage));
            Assert.That(rule.AllowedResources, Is.EqualTo(ResourceTypeMask.NoneResource | ResourceTypeMask.Ore));
        }

        [Test]
        public void TryGetRule_ReturnsFalse_WhenMissingOrNull()
        {
            BuildableRuleData[] rules =
            {
                new BuildableRuleData { StateId = (int)GridStateId.Conveyor, AllowedResources = ResourceTypeMask.NoneResource },
            };

            Assert.That(BuildableRules.TryGetRule(rules, (int)GridStateId.PowerPole, out BuildableRuleData missingRule), Is.False);
            Assert.That(missingRule.StateId, Is.EqualTo(0));

            Assert.That(BuildableRules.TryGetRule(null, (int)GridStateId.PowerPole, out BuildableRuleData nullRule), Is.False);
            Assert.That(nullRule.StateId, Is.EqualTo(0));
        }
    }
}
