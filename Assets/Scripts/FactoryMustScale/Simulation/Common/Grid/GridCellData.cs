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
    /// - Process systems should store dynamic counters (for example process progress ticks) in payload
    ///   channels, while this struct stores the static process profile id in VariantCode bits.
    /// </summary>
    public struct GridCellData
    {
        private const int OrientationShift = 0;
        private const int OrientationMask = 0b111;

        private const int VariantCodeShift = 3;
        private const int VariantCodeMask = 0b111_1111;

        private const int ShapeBits = 2;
        private const int ShapeMask = (1 << ShapeBits) - 1; // 0b11

        private const int Variant5Bits = 5;
        private const int Variant5Shift = ShapeBits; // 2
        private const int Variant5Mask = (1 << Variant5Bits) - 1; // 0b1_1111

        private const int BuildStageShift = 10;
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

        // Optional orientation/shape/variant channel for the same StateId.
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

        /// <summary>
        /// VariantCode (7 bits) is subdivided into:
        /// - ShapeCode: low 2 bits (0..3)
        /// - Variant5: high 5 bits (0..31)
        /// Encoding: VariantCode = (Variant5 << 2) | ShapeCode
        /// </summary>
        public static byte GetShapeCode(int variantId)
        {
            return (byte)(GetVariantCode(variantId) & ShapeMask);
        }

        public static int SetShapeCode(int variantId, byte shapeCode)
        {
            byte code = GetVariantCode(variantId);
            code = (byte)((code & ~ShapeMask) | (shapeCode & ShapeMask));
            return SetVariantCode(variantId, code);
        }

        public static byte GetVariant5(int variantId)
        {
            return (byte)((GetVariantCode(variantId) >> Variant5Shift) & Variant5Mask);
        }

        public static int SetVariant5(int variantId, byte variant5)
        {
            byte code = GetVariantCode(variantId);
            code = (byte)((code & ShapeMask) | ((variant5 & Variant5Mask) << Variant5Shift));
            return SetVariantCode(variantId, code);
        }

        /// <summary>
        /// Convenience helper to set both Variant5 and ShapeCode at once.
        /// </summary>
        public static int SetVariant5AndShape(int variantId, byte variant5, byte shapeCode)
        {
            byte code = (byte)(((variant5 & Variant5Mask) << Variant5Shift) | (shapeCode & ShapeMask));
            return SetVariantCode(variantId, code);
        }

        /// <summary>
        /// Reads the process profile id packed in VariantCode bits.
        /// This value maps a placed factory cell to one baked process definition.
        /// </summary>
        public static byte GetProcessProfileId(int variantId)
        {
            return GetVariant5(variantId);
        }

        public static int SetVariantCode(int variantId, byte variantCode)
        {
            int normalizedVariantCode = variantCode & VariantCodeMask;
            int clearedVariantId = variantId & ~(VariantCodeMask << VariantCodeShift);
            return clearedVariantId | (normalizedVariantCode << VariantCodeShift);
        }

        /// <summary>
        /// Packs the 5-bit process profile id (0..31) into VariantCode bits.
        /// ShapeCode is preserved.
        /// </summary>
        public static int SetProcessProfileId(int variantId, byte processProfileId)
        {
            return SetVariant5(variantId, processProfileId);
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
