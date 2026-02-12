namespace FactoryMustScale.Simulation
{
    /// <summary>
    /// Resolves process-slot direction masks into world-facing directions by combining:
    /// - slot-local direction intent (defined in process data), and
    /// - the placed cell orientation (stored in GridCellData.VariantId).
    ///
    /// Why this helper exists:
    /// - Prevents parameter-order mistakes when process ticking code asks
    ///   "which world directions are valid for this slot on this cell?"
    /// - Keeps orientation math in one deterministic place.
    ///
    /// Orientation rule:
    /// - Slot directions are authored in local cell space.
    /// - Cell orientation rotates these local directions clockwise in 90-degree steps.
    ///   Up(0 turns), Right(1), Down(2), Left(3).
    /// - Non-cardinal orientations are considered invalid for process-slot direction resolution.
    /// </summary>
    public static class FactoryProcessDirectionResolver
    {
        public static bool TryResolveWorldDirections(CardinalDirectionMask localDirections, int cellVariantId, out CardinalDirectionMask worldDirections)
        {
            CellOrientation orientation = GridCellData.GetOrientationEnum(cellVariantId);
            return TryResolveWorldDirections(localDirections, orientation, out worldDirections);
        }

        public static bool TryResolveWorldDirections(CardinalDirectionMask localDirections, CellOrientation orientation, out CardinalDirectionMask worldDirections)
        {
            int quarterTurns;
            if (!TryGetQuarterTurns(orientation, out quarterTurns))
            {
                worldDirections = CardinalDirectionMask.None;
                return false;
            }

            worldDirections = RotateMaskClockwise(localDirections, quarterTurns);
            return true;
        }

        private static bool TryGetQuarterTurns(CellOrientation orientation, out int quarterTurns)
        {
            switch (orientation)
            {
                case CellOrientation.Up:
                    quarterTurns = 0;
                    return true;
                case CellOrientation.Right:
                    quarterTurns = 1;
                    return true;
                case CellOrientation.Down:
                    quarterTurns = 2;
                    return true;
                case CellOrientation.Left:
                    quarterTurns = 3;
                    return true;
                default:
                    quarterTurns = 0;
                    return false;
            }
        }

        private static CardinalDirectionMask RotateMaskClockwise(CardinalDirectionMask mask, int quarterTurns)
        {
            int turns = quarterTurns & 0b11;
            CardinalDirectionMask rotated = CardinalDirectionMask.None;

            if ((mask & CardinalDirectionMask.Up) != 0)
            {
                rotated |= RotateOne(CardinalDirectionMask.Up, turns);
            }

            if ((mask & CardinalDirectionMask.Right) != 0)
            {
                rotated |= RotateOne(CardinalDirectionMask.Right, turns);
            }

            if ((mask & CardinalDirectionMask.Down) != 0)
            {
                rotated |= RotateOne(CardinalDirectionMask.Down, turns);
            }

            if ((mask & CardinalDirectionMask.Left) != 0)
            {
                rotated |= RotateOne(CardinalDirectionMask.Left, turns);
            }

            return rotated;
        }

        private static CardinalDirectionMask RotateOne(CardinalDirectionMask direction, int turns)
        {
            switch (turns)
            {
                case 0:
                    return direction;
                case 1:
                    switch (direction)
                    {
                        case CardinalDirectionMask.Up: return CardinalDirectionMask.Right;
                        case CardinalDirectionMask.Right: return CardinalDirectionMask.Down;
                        case CardinalDirectionMask.Down: return CardinalDirectionMask.Left;
                        case CardinalDirectionMask.Left: return CardinalDirectionMask.Up;
                        default: return CardinalDirectionMask.None;
                    }
                case 2:
                    switch (direction)
                    {
                        case CardinalDirectionMask.Up: return CardinalDirectionMask.Down;
                        case CardinalDirectionMask.Right: return CardinalDirectionMask.Left;
                        case CardinalDirectionMask.Down: return CardinalDirectionMask.Up;
                        case CardinalDirectionMask.Left: return CardinalDirectionMask.Right;
                        default: return CardinalDirectionMask.None;
                    }
                default:
                    switch (direction)
                    {
                        case CardinalDirectionMask.Up: return CardinalDirectionMask.Left;
                        case CardinalDirectionMask.Right: return CardinalDirectionMask.Up;
                        case CardinalDirectionMask.Down: return CardinalDirectionMask.Right;
                        case CardinalDirectionMask.Left: return CardinalDirectionMask.Down;
                        default: return CardinalDirectionMask.None;
                    }
            }
        }
    }
}
