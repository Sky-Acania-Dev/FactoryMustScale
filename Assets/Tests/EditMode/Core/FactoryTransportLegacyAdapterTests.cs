using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Transport;
using FactoryMustScale.Simulation.Item;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class FactoryTransportLegacyAdapterTests
    {
        [Test]
        public void SimLoop_AdapterRunsOnlyOnFactoryTicks()
        {
            FactoryCoreLoopState initialState = new FactoryCoreLoopState
            {
                ItemTransportAlgorithm = ItemTransportAlgorithm.SimplePush,
            };

            var adapter = new FactoryTransportLegacyAdapter(in initialState);
            var loop = new SimLoop(new ISimPhaseSystem[] { adapter });

            for (int i = 0; i < 8; i++)
            {
                loop.Tick();
            }

            Assert.That(adapter.ExternalIngestRunCount, Is.EqualTo(2));
            Assert.That(adapter.ComputeRunCount, Is.EqualTo(2));
            Assert.That(adapter.CommitRunCount, Is.EqualTo(2));
        }
    }
}
