namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Defines the contract for a simulation system that participates in a time-stepped simulation. Provides methods
    /// for the 3 phases: 1) ingesting external inputs, 2) performing computation, and 3) committing state changes at a given simulation clock
    /// step (tick).
    /// </summary>
    /// <remarks>Implementations of this interface are expected to process simulation logic in discrete
    /// phases, coordinated by the provided simulation clock. Each method should be called in sequence within a
    /// simulation step: first ExternalIngest, then Compute, and finally Commit. This interface is intended for use in
    /// simulation frameworks where multiple systems advance together in lockstep.</remarks>
    public interface ISimSystem
    {
        void ExternalIngest(in SimClock clock);

        void Compute(in SimClock clock);

        void Commit(in SimClock clock);
    }
}
