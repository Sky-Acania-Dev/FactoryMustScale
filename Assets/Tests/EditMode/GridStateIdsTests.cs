using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class GridStateIdsTests
    {
        [Test]
        public void GridStateId_ContainsExpectedFactoryAndTerrainEntries()
        {
            Assert.That((int)GridStateId.Conveyor, Is.EqualTo(1));
            Assert.That((int)GridStateId.Miner, Is.EqualTo(10));
            Assert.That((int)GridStateId.CrafterCore, Is.EqualTo(11));
            Assert.That((int)GridStateId.Storage, Is.EqualTo(20));
            Assert.That((int)GridStateId.PowerGenerator, Is.EqualTo(21));
            Assert.That((int)GridStateId.PowerPole, Is.EqualTo(22));

            Assert.That((int)GridStateId.TerrainGround, Is.EqualTo(101));
            Assert.That((int)GridStateId.TerrainResourceDeposit, Is.EqualTo(105));
            Assert.That((int)GridStateId.TerrainGeothermalSite, Is.EqualTo(106));
        }
    }
}
