namespace FactoryMustScale.Simulation
{
    public enum CellOrientation : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
        UpRight = 4,
        DownRight = 5,
        DownLeft = 6,
        UpLeft = 7,
        Invalid = 255
    }

    /// <summary>
    /// Practical v1 runtime payload for one deterministic simulation cell.
    ///
    /// Use-case guidance:
    /// - StateId: what the cell is (for example conveyor, crafter core, crafter input/output port).
    /// - VariantId: packed variant data (suggested: orientation + subtype + build/destruct stage).
    /// - Flags: fast booleans (powered, connected, can accept payload, can output payload, blocked, etc...).
    /// - LastUpdatedTick: deterministic "last touched" marker for debugging/extraction.
    /// - ChangeCount: monotonically increasing per-cell version for dirty checks.
    ///
    /// Multi-cell factory note:
    /// - A single machine can span multiple cells by assigning different StateId/VariantId/Flags values
    ///   to each occupied tile (for example separate input and output cells for one crafter).
    ///
    /// Payload note:
    /// - Optional per-cell dynamic payload data should live in layer-owned payload channels that share
    ///   the same index scheme, keeping this struct compact and cache-friendly.
    /// </summary>
    public struct GridCellData
    {
        private const int OrientationShift = 0;
        private const int OrientationMask = 0b11;

        private const int VariantCodeShift = 2;
        private const int VariantCodeMask = 0b111_1111;

        private const int BuildStageShift = 9;
        private const int BuildStageMask = 0b1111;

        public const int StageMarkedToConstruct = 0;
        public const int StageConstructionStart = 1;
        public const int StageConstructionEnd = 6;
        public const int StageFullyBuilt = 7;
        public const int StageDestructionStart = 8;
        public const int StageDestructionEnd = 14;
        public const int StageDestroyed = 15;

        public const uint FlagPowered = 1u << 0;
        public const uint FlagConnected = 1u << 1;
        public const uint FlagCanAcceptPayload = 1u << 2;
        public const uint FlagCanOutputPayload = 1u << 3;
        public const uint FlagBlocked = 1u << 4;

        // Authoritative structural state for this cell (empty, conveyor, crafter-part, etc...).
        public int StateId;

        // Optional orientation/variant channel for the same StateId.
        public int VariantId;

        // Bit flags for compact booleans (powered, blocked, reserved, io marker, ...).
        public uint Flags;

        // Deterministic bookkeeping used by systems and debugging.
        public int LastUpdatedTick;
        public int ChangeCount;

        public static int GetOrientation(int variantId)
        {
            return (variantId >> OrientationShift) & OrientationMask;
        }

        public static CellOrientation GetOrientationEnum(int variantId)
        {
            int orientationValue = GetOrientation(variantId);

            if (orientationValue < (int)CellOrientation.Up || orientationValue > (int)CellOrientation.UpLeft)
            {
                return CellOrientation.Invalid;
            }

            return (CellOrientation)orientationValue;
        }

        public static int SetOrientation(int variantId, int orientation)
        {
            int clampedOrientation = orientation & OrientationMask;
            int clearedVariantId = variantId & ~(OrientationMask << OrientationShift);
            return clearedVariantId | (clampedOrientation << OrientationShift);
        }

        public static int SetOrientation(int variantId, CellOrientation orientation)
        {
            return SetOrientation(variantId, (int)orientation);
        }

        public static byte GetVariantCode(int variantId)
        {
            return (byte)((variantId >> VariantCodeShift) & VariantCodeMask);
        }

        public static int SetVariantCode(int variantId, byte variantCode)
        {
            int normalizedVariantCode = variantCode & VariantCodeMask;
            int clearedVariantId = variantId & ~(VariantCodeMask << VariantCodeShift);
            return clearedVariantId | (normalizedVariantCode << VariantCodeShift);
        }

        public static int GetConstructionDestructionStage(int variantId)
        {
            return (variantId >> BuildStageShift) & BuildStageMask;
        }

        public static int SetConstructionDestructionStage(int variantId, int stage)
        {
            int clampedStage = stage & BuildStageMask;
            int clearedVariantId = variantId & ~(BuildStageMask << BuildStageShift);
            return clearedVariantId | (clampedStage << BuildStageShift);
        }

        public static bool HasFlag(uint flags, uint flag)
        {
            return (flags & flag) != 0u;
        }

        public static bool IsPowered(uint flags)
        {
            return HasFlag(flags, FlagPowered);
        }

        public static bool IsConnected(uint flags)
        {
            return HasFlag(flags, FlagConnected);
        }

        public static bool CanAcceptPayload(uint flags)
        {
            return HasFlag(flags, FlagCanAcceptPayload);
        }

        public static bool CanOutputPayload(uint flags)
        {
            return HasFlag(flags, FlagCanOutputPayload);
        }

        public static bool IsBlocked(uint flags)
        {
            return HasFlag(flags, FlagBlocked);
        }

        public static bool IsUnderConstruction(int variantId)
        {
            int stage = GetConstructionDestructionStage(variantId);
            return stage >= StageConstructionStart && stage <= StageConstructionEnd;
        }

        public static bool IsMarkedForDestruction(int variantId)
        {
            int stage = GetConstructionDestructionStage(variantId);
            return stage >= StageDestructionStart && stage <= StageDestructionEnd;
        }
    }
}
