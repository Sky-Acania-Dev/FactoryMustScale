namespace FactoryMustScale.Simulation.Domains.Factory.Systems
{
    using FactoryMustScale.Simulation.Core;
    using FactoryMustScale.Simulation.Domains.Factory.Systems.Build;

    /// <summary>
    /// Factory domain phase orchestrator under the canonical PreCompute/Compute/Commit contract.
    ///
    /// Current wiring:
    /// - PreCompute: delegates structural edit ingest + early commit to build structural system.
    /// - Compute: delegates to build structural system (read-only placeholder for now).
    /// - Commit: delegates to build structural system (reserved for future non-structural deltas).
    ///
    /// Additional domain systems (for example transport/crafting/power) should be added here in
    /// deterministic fixed order as they are refactored into non-legacy ISimSystem components.
    /// </summary>
    public sealed class FactoryCoreLoopSystem : ISimSystem, ISimHashSource
    {
        private FactoryBuildStructuralSystem _buildStructuralSystem;

        public FactoryCoreLoopSystem(in FactoryBuildSystemState initialState)
        {
            _buildStructuralSystem = new FactoryBuildStructuralSystem(in initialState);
        }

        public FactoryBuildSystemState BuildState => _buildStructuralSystem.State;

        public void PreCompute(ref SimContext ctx)
        {
            _buildStructuralSystem.PreCompute(ref ctx);
        }

        public void Compute(ref SimContext ctx)
        {
            _buildStructuralSystem.Compute(ref ctx);
        }

        public void Commit(ref SimContext ctx)
        {
            _buildStructuralSystem.Commit(ref ctx);
        }

        public void AppendHash(ref SimHashBuilder builder)
        {
            _buildStructuralSystem.AppendHash(ref builder);
        }
    }
}
