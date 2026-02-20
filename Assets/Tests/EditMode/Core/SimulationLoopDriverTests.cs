using System.Reflection;
using FactoryMustScale.Runtime;
using FactoryMustScale.Simulation.Core;
using FactoryMustScale.Simulation.Domains.Factory.Systems.Transport;
using FactoryMustScale.Simulation.Item;
using NUnit.Framework;
using UnityEngine;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class SimulationLoopDriverTests
    {
        [Test]
        public void ConfigureFactoryTransportState_AfterAwake_RebuildsLoopAndPreservesTickContinuity()
        {
            GameObject gameObject = new GameObject("SimulationLoopDriverTests");

            try
            {
                var driver = gameObject.AddComponent<SimulationLoopDriver>();

                driver.TickOnce();
                driver.TickOnce();
                driver.TickOnce();

                Assert.That(driver.UnitTick, Is.EqualTo(3));

                FactoryCoreLoopState initialState = new FactoryCoreLoopState
                {
                    ItemTransportAlgorithm = ItemTransportAlgorithm.SimplePush,
                };

                driver.ConfigureFactoryTransportState(in initialState);
                driver.TickOnce();

                Assert.That(driver.UnitTick, Is.EqualTo(4));

                FieldInfo systemsField = typeof(SimulationLoopDriver).GetField("_systems", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(systemsField, Is.Not.Null);
                var systems = (ISimPhaseSystem[])systemsField.GetValue(driver);

                Assert.That(systems.Length, Is.EqualTo(1));
                Assert.That(systems[0], Is.TypeOf<FactoryTransportLegacyAdapter>());

                var adapter = (FactoryTransportLegacyAdapter)systems[0];
                Assert.That(adapter.ExternalIngestRunCount, Is.EqualTo(1));
                Assert.That(adapter.ComputeRunCount, Is.EqualTo(1));
                Assert.That(adapter.CommitRunCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
