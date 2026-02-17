using FactoryMustScale.Simulation;
using NUnit.Framework;
using System;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class TwoPhaseHarnessTests
    {
        private const int PayloadChannel = 0;

        private struct TwoPhaseTestState
        {
            public Layer Layer;
            public int PayloadChannelIndex;
            public bool EmitConflicts;
            public bool ReverseConflictAppendOrder;
        }

        private struct TwoPhaseTestSystem : ISimulationSystem<TwoPhaseTestState>
        {
            private const int OpSetPayload = 1;

            public void TickCommit(ref TwoPhaseTestState state, int tickIndex, ref EventBuffer prev)
            {
                int cellCount = state.Layer.Width * state.Layer.Height;
                for (int targetIndex = 0; targetIndex < cellCount; targetIndex++)
                {
                    bool found = false;
                    EventBuffer.EventRecord winner = default;

                    for (int i = 0; i < prev.Count; i++)
                    {
                        prev.TryGetAt(i, out EventBuffer.EventRecord record);
                        if (record.OpCode != OpSetPayload || record.TargetIndex != targetIndex)
                        {
                            continue;
                        }

                        if (!found || record.SourceIndex < winner.SourceIndex)
                        {
                            winner = record;
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        continue;
                    }

                    GetPositionFromIndex(ref state, winner.TargetIndex, out int x, out int y);
                    state.Layer.TrySetPayload(x, y, state.PayloadChannelIndex, winner.A);
                }
            }

            public void TickCompute(ref TwoPhaseTestState state, int tickIndex, ref EventBuffer next)
            {
                int cellCount = state.Layer.Width * state.Layer.Height;
                for (int index = 0; index < cellCount; index++)
                {
                    GetPositionFromIndex(ref state, index, out int x, out int y);
                    state.Layer.TryGetPayload(x, y, state.PayloadChannelIndex, out int payload);
                    if (payload == 0)
                    {
                        EventBuffer.EventRecord eventRecord = default;
                        eventRecord.TargetIndex = index;
                        eventRecord.OpCode = OpSetPayload;
                        eventRecord.A = tickIndex + 1;
                        eventRecord.B = 0;
                        eventRecord.SourceIndex = index;
                        next.Append(eventRecord);
                    }
                }

                if (!state.EmitConflicts)
                {
                    return;
                }

                AppendConflictEvents(ref next, state.ReverseConflictAppendOrder);
            }

            private static void AppendConflictEvents(ref EventBuffer next, bool reverseOrder)
            {
                EventBuffer.EventRecord lowSource = default;
                lowSource.TargetIndex = 0;
                lowSource.OpCode = OpSetPayload;
                lowSource.A = 100;
                lowSource.SourceIndex = 1;

                EventBuffer.EventRecord highSource = default;
                highSource.TargetIndex = 0;
                highSource.OpCode = OpSetPayload;
                highSource.A = 200;
                highSource.SourceIndex = 2;

                if (reverseOrder)
                {
                    next.Append(highSource);
                    next.Append(lowSource);
                    return;
                }

                next.Append(lowSource);
                next.Append(highSource);
            }

            private static void GetPositionFromIndex(ref TwoPhaseTestState state, int index, out int x, out int y)
            {
                int width = state.Layer.Width;
                int localY = index / width;
                int localX = index - (localY * width);
                x = state.Layer.MinX + localX;
                y = state.Layer.MinY + localY;
            }
        }

        [Test]
        public void ComputePhase_DoesNotMutateAuthoritativeState()
        {
            TwoPhaseTestState state = CreateState(2, 1);
            EventBuffer events = new EventBuffer(16);

            int before = ReadPayload(state, 0);
            new TwoPhaseTestSystem().TickCompute(ref state, 0, ref events);
            int afterCompute = ReadPayload(state, 0);

            Assert.That(afterCompute, Is.EqualTo(before));
            Assert.That(events.Count, Is.GreaterThan(0));

            new TwoPhaseTestSystem().TickCommit(ref state, 1, ref events);
            int afterCommit = ReadPayload(state, 0);

            Assert.That(afterCommit, Is.EqualTo(1));
        }

        [Test]
        public void TwoPhaseHarness_IsDeterministicAcrossRepeatedRuns()
        {
            const int ticks = 12;
            var first = RunHarness(ticks);
            var second = RunHarness(ticks);

            Assert.That(first.StateSignature, Is.EqualTo(second.StateSignature));
            Assert.That(first.EventSignature, Is.EqualTo(second.EventSignature));
        }

        [Test]
        public void TwoPhaseHarness_AllocatesZeroBytesPerTick_AfterWarmup()
        {
            TwoPhaseTestState state = CreateState(8, 8);
            var harness = new FixedStepSimulationHarness<TwoPhaseTestState, TwoPhaseTestSystem>(
                state,
                new TwoPhaseTestSystem(),
                256);

            harness.Tick(5);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();
            harness.Tick(64);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0));
        }

        [Test]
        public void CommitConflictResolution_UsesLowestSourceIndex_RegardlessOfAppendOrder()
        {
            TwoPhaseTestState firstState = CreateState(1, 1);
            EventBuffer first = new EventBuffer(8);
            AppendConflictEvents(ref first, true);
            new TwoPhaseTestSystem().TickCommit(ref firstState, 0, ref first);

            TwoPhaseTestState secondState = CreateState(1, 1);
            EventBuffer second = new EventBuffer(8);
            AppendConflictEvents(ref second, false);
            new TwoPhaseTestSystem().TickCommit(ref secondState, 0, ref second);

            Assert.That(ReadPayload(firstState, 0), Is.EqualTo(100));
            Assert.That(ReadPayload(secondState, 0), Is.EqualTo(100));
        }

        private static (int StateSignature, int EventSignature) RunHarness(int ticks)
        {
            TwoPhaseTestState state = CreateState(4, 4);
            var harness = new FixedStepSimulationHarness<TwoPhaseTestState, TwoPhaseTestSystem>(
                state,
                new TwoPhaseTestSystem(),
                128);

            int eventSignature = 0;
            for (int i = 0; i < ticks; i++)
            {
                harness.Tick(1);
                EventBuffer commit = harness.CommitBuffer;
                for (int j = 0; j < commit.Count; j++)
                {
                    commit.TryGetAt(j, out EventBuffer.EventRecord record);
                    unchecked
                    {
                        eventSignature = (eventSignature * 31) + record.TargetIndex;
                        eventSignature = (eventSignature * 31) + record.A;
                        eventSignature = (eventSignature * 31) + record.SourceIndex;
                    }
                }
            }

            int stateSignature = 0;
            int cellCount = harness.State.Layer.Width * harness.State.Layer.Height;
            for (int index = 0; index < cellCount; index++)
            {
                int payload = ReadPayload(harness.State, index);
                unchecked
                {
                    stateSignature = (stateSignature * 33) + payload;
                }
            }

            return (stateSignature, eventSignature);
        }

        private static void AppendConflictEvents(ref EventBuffer events, bool reverseOrder)
        {
            EventBuffer.EventRecord highSource = default;
            highSource.TargetIndex = 0;
            highSource.OpCode = 1;
            highSource.A = 200;
            highSource.SourceIndex = 2;

            EventBuffer.EventRecord lowSource = default;
            lowSource.TargetIndex = 0;
            lowSource.OpCode = 1;
            lowSource.A = 100;
            lowSource.SourceIndex = 1;

            if (reverseOrder)
            {
                events.Append(highSource);
                events.Append(lowSource);
                return;
            }

            events.Append(lowSource);
            events.Append(highSource);
        }

        private static TwoPhaseTestState CreateState(int width, int height)
        {
            return new TwoPhaseTestState
            {
                Layer = new Layer(0, 0, width, height, 1),
                PayloadChannelIndex = PayloadChannel,
                EmitConflicts = false,
                ReverseConflictAppendOrder = false,
            };
        }

        private static int ReadPayload(TwoPhaseTestState state, int index)
        {
            int localY = index / state.Layer.Width;
            int localX = index - (localY * state.Layer.Width);
            int x = state.Layer.MinX + localX;
            int y = state.Layer.MinY + localY;
            state.Layer.TryGetPayload(x, y, state.PayloadChannelIndex, out int payload);
            return payload;
        }
    }
}
