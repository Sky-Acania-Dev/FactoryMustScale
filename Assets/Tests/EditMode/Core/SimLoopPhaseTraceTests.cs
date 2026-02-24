using FactoryMustScale.Simulation.Core;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode.Core
{
    public sealed class SimLoopPhaseTraceTests
    {
        [Test]
        public void Tick_RecordsCanonicalPhaseOrder()
        {
            var smoke = new SimLoopSmokeTestSystem();
            var loop = new SimLoop(new ISimSystem[] { smoke }, traceCapacity: 8);

            loop.Tick();

            Assert.That(smoke.LastObservedTick, Is.EqualTo(1));
            Assert.That(smoke.LastPhaseMarker, Is.EqualTo(3));
            Assert.That(loop.PhaseTraceCount, Is.EqualTo(3));

            Assert.That(loop.TryGetPhaseTraceAt(0, out int trace0), Is.True);
            Assert.That(loop.TryGetPhaseTraceAt(1, out int trace1), Is.True);
            Assert.That(loop.TryGetPhaseTraceAt(2, out int trace2), Is.True);

            Assert.That(trace0, Is.EqualTo((1 * 10) + (int)SimPhase.PreCompute));
            Assert.That(trace1, Is.EqualTo((1 * 10) + (int)SimPhase.Compute));
            Assert.That(trace2, Is.EqualTo((1 * 10) + (int)SimPhase.Commit));
        }

        [Test]
        public void Tick_MultipleTicksRemainDeterministic()
        {
            var loopA = new SimLoop(new ISimSystem[] { new SimLoopSmokeTestSystem() }, traceCapacity: 8);
            var loopB = new SimLoop(new ISimSystem[] { new SimLoopSmokeTestSystem() }, traceCapacity: 8);

            loopA.Tick();
            loopA.Tick();
            loopB.Tick();
            loopB.Tick();

            Assert.That(loopA.PhaseTraceCount, Is.EqualTo(3));
            Assert.That(loopB.PhaseTraceCount, Is.EqualTo(3));

            for (int i = 0; i < 3; i++)
            {
                Assert.That(loopA.TryGetPhaseTraceAt(i, out int a), Is.True);
                Assert.That(loopB.TryGetPhaseTraceAt(i, out int b), Is.True);
                Assert.That(a, Is.EqualTo(b));
            }
        }
    }
}
