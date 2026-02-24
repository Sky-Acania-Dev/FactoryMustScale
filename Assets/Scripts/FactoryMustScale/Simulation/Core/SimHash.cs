namespace FactoryMustScale.Simulation.Core
{
    /// <summary>
    /// Deterministic state hash helper for post-commit instrumentation.
    /// </summary>
    public static class SimHash
    {
        public static ulong ComputeHash(in SimContext context)
        {
            SimHashBuilder builder = SimHashBuilder.Create();
            builder.AppendInt(context.Clock.UnitTick);

            int sourceCount = context.HashSourceCount;
            for (int i = 0; i < sourceCount; i++)
            {
                if (context.TryGetHashSource(i, out ISimHashSource source))
                {
                    source.AppendHash(ref builder);
                }
            }

            return builder.ToHash();
        }
    }

    public struct SimHashBuilder
    {
        private const ulong Offset = 1469598103934665603UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _value;

        public static SimHashBuilder Create()
        {
            SimHashBuilder builder;
            builder._value = Offset;
            return builder;
        }

        public void AppendInt(int value)
        {
            AppendUInt(unchecked((uint)value));
        }

        public void AppendUInt(uint value)
        {
            unchecked
            {
                _value ^= value & 0xFFu;
                _value *= Prime;
                _value ^= (value >> 8) & 0xFFu;
                _value *= Prime;
                _value ^= (value >> 16) & 0xFFu;
                _value *= Prime;
                _value ^= (value >> 24) & 0xFFu;
                _value *= Prime;
            }
        }

        public void AppendULong(ulong value)
        {
            AppendUInt((uint)value);
            AppendUInt((uint)(value >> 32));
        }

        public ulong ToHash()
        {
            return _value;
        }
    }
}
