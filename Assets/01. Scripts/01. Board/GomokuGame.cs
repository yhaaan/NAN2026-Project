using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    public enum StoneColor
    {
        None,
        Black,
        White
    }

    public enum GamePhase
    {
        Placement,
        Combat,
        GameOver
    }

    public sealed class GomokuGame
    {
        public const int BoardSize = 15;
        private const int StonesToWin = 5;

        private static readonly (int x, int y)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        private readonly BoardUnit[,] board = new BoardUnit[BoardSize, BoardSize];
        private readonly List<BoardUnit> units = new List<BoardUnit>();
        private int nextPlacementOrder;
        private int placementsInCycle;

        public StoneColor StartingSide { get; private set; } = StoneColor.Black;
        public StoneColor CurrentTurn { get; private set; } = StoneColor.Black;
        public StoneColor Winner { get; private set; } = StoneColor.None;
        public GamePhase Phase { get; private set; } = GamePhase.Placement;
        public int LastMoveX { get; private set; } = -1;
        public int LastMoveY { get; private set; } = -1;
        public bool IsGameOver => Phase == GamePhase.GameOver;
        public IReadOnlyList<BoardUnit> Units => units;

        public GomokuGame()
        {
            StartNewGame(StoneColor.Black);
        }

        public void StartNewGame(StoneColor startingSide)
        {
            if (startingSide == StoneColor.None)
            {
                throw new ArgumentException("A game must start with Black or White.", nameof(startingSide));
            }

            Array.Clear(board, 0, board.Length);
            units.Clear();
            StartingSide = startingSide;
            CurrentTurn = startingSide;
            Winner = StoneColor.None;
            Phase = GamePhase.Placement;
            LastMoveX = -1;
            LastMoveY = -1;
            nextPlacementOrder = 0;
            placementsInCycle = 0;
        }

        public StoneColor GetStone(int x, int y)
        {
            return GetUnit(x, y)?.Side ?? StoneColor.None;
        }

        public BoardUnit GetUnit(int x, int y)
        {
            ValidateCoordinates(x, y);
            return board[x, y];
        }

        public bool TryPlace(int x, int y, UnitDefinitionSO definition)
        {
            if (Phase != GamePhase.Placement
                || definition == null
                || !IsInsideBoard(x, y)
                || board[x, y] != null)
            {
                return false;
            }

            StoneColor placedSide = CurrentTurn;
            var unit = new BoardUnit(definition, placedSide, x, y, nextPlacementOrder++);
            board[x, y] = unit;
            units.Add(unit);
            LastMoveX = x;
            LastMoveY = y;

            if (HasFiveFrom(x, y, placedSide))
            {
                Winner = placedSide;
                Phase = GamePhase.GameOver;
                return true;
            }

            if (units.Count >= BoardSize * BoardSize)
            {
                Winner = StoneColor.None;
                Phase = GamePhase.GameOver;
                return true;
            }

            placementsInCycle++;
            if (placementsInCycle >= 2)
            {
                Phase = GamePhase.Combat;
            }
            else
            {
                CurrentTurn = OpponentOf(placedSide);
            }

            return true;
        }

        public void CompleteCombat()
        {
            if (Phase != GamePhase.Combat)
            {
                return;
            }

            placementsInCycle = 0;
            CurrentTurn = StartingSide;
            Phase = GamePhase.Placement;
        }

        public void RemoveUnit(BoardUnit unit)
        {
            if (unit == null || !IsInsideBoard(unit.X, unit.Y) || board[unit.X, unit.Y] != unit)
            {
                return;
            }

            board[unit.X, unit.Y] = null;
            units.Remove(unit);
        }

        public bool HasAnyUnits(StoneColor side)
        {
            foreach (BoardUnit unit in units)
            {
                if (unit.Side == side && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public bool WouldWin(int x, int y, StoneColor side)
        {
            if (side == StoneColor.None || !IsInsideBoard(x, y) || board[x, y] != null)
            {
                return false;
            }

            foreach ((int directionX, int directionY) in Directions)
            {
                int connected = 1
                    + CountDirection(x, y, directionX, directionY, side)
                    + CountDirection(x, y, -directionX, -directionY, side);

                if (connected >= StonesToWin)
                {
                    return true;
                }
            }

            return false;
        }

        public void Restart()
        {
            StartNewGame(StoneColor.Black);
        }

        public static StoneColor OpponentOf(StoneColor side)
        {
            return side == StoneColor.Black ? StoneColor.White : StoneColor.Black;
        }

        private bool HasFiveFrom(int x, int y, StoneColor side)
        {
            foreach ((int directionX, int directionY) in Directions)
            {
                int connected = 1
                    + CountDirection(x, y, directionX, directionY, side)
                    + CountDirection(x, y, -directionX, -directionY, side);

                if (connected >= StonesToWin)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountDirection(int startX, int startY, int stepX, int stepY, StoneColor side)
        {
            int count = 0;
            int x = startX + stepX;
            int y = startY + stepY;

            while (IsInsideBoard(x, y) && GetStone(x, y) == side)
            {
                count++;
                x += stepX;
                y += stepY;
            }

            return count;
        }

        private static bool IsInsideBoard(int x, int y)
        {
            return x >= 0 && x < BoardSize && y >= 0 && y < BoardSize;
        }

        private static void ValidateCoordinates(int x, int y)
        {
            if (!IsInsideBoard(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Board coordinates must be between 0 and {BoardSize - 1}.");
            }
        }
    }
}
