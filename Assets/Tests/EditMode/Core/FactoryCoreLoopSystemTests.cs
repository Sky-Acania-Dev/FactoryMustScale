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
            Assert.That(harness.State.PhaseTraceCount, Is.EqualTo(3));
            Assert.That(harness.State.PhaseTraceBuffer[0], Is.EqualTo((0 * 10) + (int)FactoryTickStep.InputAndEventHandling));
            Assert.That(harness.State.PhaseTraceBuffer[1], Is.EqualTo((0 * 10) + (int)FactoryTickStep.CellProcessUpdate));
            Assert.That(harness.State.PhaseTraceBuffer[2], Is.EqualTo((0 * 10) + (int)FactoryTickStep.PublishEventsForNextTick));
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
            Assert.That(harness.State.InputAndEventHandlingCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.CellProcessUpdateCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.PublishEventsForNextTickCount, Is.EqualTo(maxTicks));
            Assert.That(harness.State.PhaseTraceCount, Is.EqualTo(maxTicks * 3));
        }
    }
}
