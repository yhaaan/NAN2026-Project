using System;

namespace NAN2026.Gomoku
{
    public enum BoardPointerMode
    {
        None,
        UnitHover,
        PlacementPreview
    }

    public readonly struct BoardPointerState : IEquatable<BoardPointerState>
    {
        public static BoardPointerState None { get; } = new BoardPointerState(
            BoardPointerMode.None,
            null,
            -1,
            -1);

        public BoardPointerMode Mode { get; }
        public BoardUnit HoveredUnit { get; }
        public int X { get; }
        public int Y { get; }

        private BoardPointerState(BoardPointerMode mode, BoardUnit hoveredUnit, int x, int y)
        {
            Mode = mode;
            HoveredUnit = hoveredUnit;
            X = x;
            Y = y;
        }

        public static BoardPointerState ForUnit(BoardUnit unit)
        {
            return unit == null
                ? None
                : new BoardPointerState(BoardPointerMode.UnitHover, unit, unit.X, unit.Y);
        }

        public static BoardPointerState ForPlacement(int x, int y)
        {
            return new BoardPointerState(BoardPointerMode.PlacementPreview, null, x, y);
        }

        public bool Equals(BoardPointerState other)
        {
            if (Mode != other.Mode)
            {
                return false;
            }

            return Mode switch
            {
                BoardPointerMode.UnitHover => ReferenceEquals(HoveredUnit, other.HoveredUnit),
                BoardPointerMode.PlacementPreview => X == other.X && Y == other.Y,
                _ => true
            };
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPointerState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                switch (Mode)
                {
                    case BoardPointerMode.UnitHover:
                        return ((int)Mode * 397) ^ (HoveredUnit != null ? HoveredUnit.GetHashCode() : 0);
                    case BoardPointerMode.PlacementPreview:
                        int hashCode = ((int)Mode * 397) ^ X;
                        hashCode = (hashCode * 397) ^ Y;
                        return hashCode;
                    default:
                        return (int)Mode;
                }
            }
        }
    }
}
