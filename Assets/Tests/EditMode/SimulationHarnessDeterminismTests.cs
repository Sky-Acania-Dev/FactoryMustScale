using FactoryMustScale.Simulation;
using FactoryMustScale.Simulation.Legacy;
using NUnit.Framework;
using System;

namespace FactoryMustScale.Tests.EditMode
{
    /// <summary>
    /// EditMode tests that codify baseline simulation constraints from AGENTS.md and Docs/SimRules.md.
    ///
    /// Why this test suite exists:
    /// - Guards fixed-step behavior through explicit tick-count assertions.
    /// - Guards determinism through repeat-run equivalence checks.
    /// - Guards hot-path memory behavior through per-tick allocation checks.
    ///
    /// Assumptions:
    /// - <see cref="GC.GetAllocatedBytesForCurrentThread"/> is available in the target Unity/.NET runtime.
    /// - Allocation test executes on one thread and reflects managed allocations for this workload.
    ///
    /// Possible improvement relative to rules:
    /// - Add additional deterministic scenarios with explicit input-buffer snapshots once ingestion/extraction phases are added.
    /// </summary>
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
