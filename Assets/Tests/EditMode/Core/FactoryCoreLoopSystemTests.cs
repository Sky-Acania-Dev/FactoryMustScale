using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Core;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class FactoryCoreLoopSystemTests
    {
        [Test]
        public void Tick_RunsPhasesInDeterministicOrder()
        {
            var initialState = new FactoryCoreLoopState
            {
                Running = true,
                MaxFactoryTicks = 5,
                FactoryTicksExecuted = 0,
                PhaseTraceBuffer = new int[10],
                PhaseTraceCount = 0,
            };

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(1);

            Assert.That(harness.State.FactoryTicksExecuted, Is.EqualTo(1));
            Assert.That(harness.State.PhaseTraceCount, Is.EqualTo(5));
            Assert.That(harness.State.PhaseTraceBuffer[0], Is.EqualTo((0 * 10) + (int)FactoryTickStep.IngestEvents));
            Assert.That(harness.State.PhaseTraceBuffer[1], Is.EqualTo((0 * 10) + (int)FactoryTickStep.ApplyEvents));
            Assert.That(harness.State.PhaseTraceBuffer[2], Is.EqualTo((0 * 10) + (int)FactoryTickStep.PrepareSimulation));
            Assert.That(harness.State.PhaseTraceBuffer[3], Is.EqualTo((0 * 10) + (int)FactoryTickStep.RunSimulation));
            Assert.That(harness.State.PhaseTraceBuffer[4], Is.EqualTo((0 * 10) + (int)FactoryTickStep.ExtractOutputs));
        }

        [Test]
        public void Tick_UpdatesPerPhaseCountersAndStopsAtMaxTicks()
        {
            const int maxTicks = 3;

            var initialState = new FactoryCoreLoopState
            {
                Running = true,
                MaxFactoryTicks = maxTicks,
                FactoryTicksExecuted = 0,
                PhaseTraceBuffer = new int[20],
                PhaseTraceCount = 0,
            };

            var harness = new FixedStepSimulationHarness<FactoryCoreLoopState, FactoryCoreLoopSystem>(
                initialState,
                new FactoryCoreLoopSystem());

            harness.Tick(maxTicks + 2);

            Assert.That(harness.State.FactoryTicksExecuted, Is.EqualTo(maxTicks));
            Assert.That(harness.State.Running, Is.False);
            Assert.That(harness.State.IngestEventsCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.ApplyEventsCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.PrepareSimulationCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.RunSimulationCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.ExtractOutputsCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.PhaseTraceCount, Is.EqualTo(maxTicks * 5));
        }
    }
}
