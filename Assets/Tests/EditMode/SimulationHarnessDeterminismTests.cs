using FactoryMustScale.Simulation;
using NUnit.Framework;
using System;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class SimulationHarnessDeterminismTests
    {
        [Test]
        public void DummySystem_ReachesExpectedFinalState_AfterFixedTickCount()
        {
            const int ticksToRun = 128;

            var initialState = new DummySimulationState
            {
                Counter = 1,
                Checksum = 7,
            };

            var harness = new FixedStepSimulationHarness<DummySimulationState, DummyDeterministicSimulationSystem>(
                initialState,
                new DummyDeterministicSimulationSystem());

            harness.Tick(ticksToRun);

            Assert.That(harness.CurrentTick, Is.EqualTo(ticksToRun));
            Assert.That(harness.State.Counter, Is.EqualTo(385));
            Assert.That(harness.State.Checksum, Is.EqualTo(1474803079));
        }

        [Test]
        public void DummySystem_IsDeterministic_ForSameInitialStateAndTickCount()
        {
            const int ticksToRun = 256;

            var initialState = new DummySimulationState
            {
                Counter = 10,
                Checksum = 42,
            };

            var firstHarness = new FixedStepSimulationHarness<DummySimulationState, DummyDeterministicSimulationSystem>(
                initialState,
                new DummyDeterministicSimulationSystem());

            var secondHarness = new FixedStepSimulationHarness<DummySimulationState, DummyDeterministicSimulationSystem>(
                initialState,
                new DummyDeterministicSimulationSystem());

            firstHarness.Tick(ticksToRun);
            secondHarness.Tick(ticksToRun);

            Assert.That(firstHarness.State.Counter, Is.EqualTo(secondHarness.State.Counter));
            Assert.That(firstHarness.State.Checksum, Is.EqualTo(secondHarness.State.Checksum));
        }

        [Test]
        public void DummySystem_AllocatesZeroBytes_PerTickAfterWarmup()
        {
            const int warmupTicks = 16;
            const int measuredTicks = 256;

            var initialState = new DummySimulationState
            {
                Counter = 3,
                Checksum = 5,
            };

            var harness = new FixedStepSimulationHarness<DummySimulationState, DummyDeterministicSimulationSystem>(
                initialState,
                new DummyDeterministicSimulationSystem());

            harness.Tick(warmupTicks);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            harness.Tick(measuredTicks);
            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(allocatedAfter - allocatedBefore, Is.EqualTo(0));
        }
    }
}
