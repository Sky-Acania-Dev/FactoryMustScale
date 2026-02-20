using System;

namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Executes a simulation system on a deterministic fixed tick sequence.
    ///
    /// Why this class exists:
    /// - Provides the smallest reusable harness needed to run pure simulation logic outside MonoBehaviours.
    /// - Encodes fixed-step progression explicitly as integer ticks, matching project rules that simulation must not be frame-driven.
    ///
    /// Determinism strategy:
    /// - Stable, indexed <c>for</c> loop for tick progression.
    /// - Monotonic integer tick counter passed into the system each step.
    /// - No dependence on time delta, rendering loop, or unordered data structures.
    ///
    /// Forbidden-construct avoidance:
    /// - No UnityEngine APIs.
    /// - No LINQ, no <c>foreach</c>, no per-tick heap allocation in this loop.
    /// - System instance is a struct generic to avoid interface boxing during tick calls.
    ///
    /// Assumptions:
    /// - <typeparamref name="TSystem"/> is deterministic and does not allocate in its <c>Tick</c> path.
    /// - <typeparamref name="TState"/> shape is controlled by caller and should remain serializable-friendly.
    ///
    /// Possible improvement relative to rules:
    /// - Current API applies only state-update phase; future revision can model input ingestion and output extraction explicitly
    ///   while preserving fixed-step and allocation-free operation.
    /// </summary>
    public sealed class FixedStepSimulationHarness<TState, TSystem>
        where TSystem : struct, ISimulationSystem<TState>
    {
        private const int DefaultEventBufferCapacity = 1024;

        private TState _state;
        private TSystem _system;
        private int _currentTick;
        private EventBuffer _eventsA;
        private EventBuffer _eventsB;
        private bool _commitReadsBufferA;

        public FixedStepSimulationHarness(TState initialState, TSystem system)
            : this(initialState, system, DefaultEventBufferCapacity)
        {
        }

        public FixedStepSimulationHarness(TState initialState, TSystem system, int eventBufferCapacity)
        {
            _state = initialState;
            _system = system;
            _currentTick = 0;
            _eventsA = new EventBuffer(eventBufferCapacity);
            _eventsB = new EventBuffer(eventBufferCapacity);
            _commitReadsBufferA = true;
        }

        public int CurrentTick => _currentTick;

        public TState State => _state;

        public EventBuffer CommitBuffer => _commitReadsBufferA ? _eventsA : _eventsB;

        public EventBuffer NextBuffer => _commitReadsBufferA ? _eventsB : _eventsA;

        public void Tick(int tickCount)
        {
            if (tickCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickCount));
            }

            for (int i = 0; i < tickCount; i++)
            {
                TickCommitOnly();
                TickComputeOnly();
                SwapBuffers();
                _currentTick++;
            }
        }

        public void TickCommitOnly()
        {
            if (_commitReadsBufferA)
            {
                _system.TickCommit(ref _state, _currentTick, ref _eventsA);
                return;
            }

            _system.TickCommit(ref _state, _currentTick, ref _eventsB);
        }

        public void TickComputeOnly()
        {
            if (_commitReadsBufferA)
            {
                _eventsB.Clear();
                _system.TickCompute(ref _state, _currentTick, ref _eventsB);
                return;
            }

            _eventsA.Clear();
            _system.TickCompute(ref _state, _currentTick, ref _eventsA);
        }

        private void SwapBuffers()
        {
            _commitReadsBufferA = !_commitReadsBufferA;
        }
    }
}
