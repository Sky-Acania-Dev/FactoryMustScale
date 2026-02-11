using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class GridStateIdsTests
    {
        [Test]
        public void GridStateId_ContainsExpectedFactoryEntries()
        {
            Assert.That((int)GridStateId.Conveyor, Is.EqualTo(1));
            Assert.That((int)GridStateId.Splitter, Is.EqualTo(2));
            Assert.That((int)GridStateId.Merger, Is.EqualTo(3));

            Assert.That((int)GridStateId.Miner, Is.EqualTo(10));
            Assert.That((int)GridStateId.CrafterCore, Is.EqualTo(11));
            Assert.That((int)GridStateId.CrafterInputPort, Is.EqualTo(12));
            Assert.That((int)GridStateId.CrafterOutputPort, Is.EqualTo(13));

            Assert.That((int)GridStateId.Storage, Is.EqualTo(20));
            Assert.That((int)GridStateId.PowerGenerator, Is.EqualTo(21));
            Assert.That((int)GridStateId.PowerPole, Is.EqualTo(22));
            Assert.That((int)GridStateId.PowerPylon, Is.EqualTo(23));
        }
    }
}
