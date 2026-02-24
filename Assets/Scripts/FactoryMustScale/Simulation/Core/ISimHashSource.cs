namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Optional deterministic hash contributor for post-commit instrumentation.
    /// </summary>
    public interface ISimHashSource
    {
        void AppendHash(ref SimHashBuilder builder);
    }
}
