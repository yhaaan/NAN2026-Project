using System;

namespace NAN2026.Gomoku
{
    public enum StoneColor
    {
        None,
        Black,
        White
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

        private readonly StoneColor[,] board = new StoneColor[BoardSize, BoardSize];

        public StoneColor CurrentTurn { get; private set; } = StoneColor.Black;
        public StoneColor Winner { get; private set; } = StoneColor.None;
        public int LastMoveX { get; private set; } = -1;
        public int LastMoveY { get; private set; } = -1;
        public bool IsGameOver => Winner != StoneColor.None;

        public StoneColor GetStone(int x, int y)
        {
            ValidateCoordinates(x, y);
            return board[x, y];
        }

        public bool TryPlace(int x, int y)
        {
            if (IsGameOver || !IsInsideBoard(x, y) || board[x, y] != StoneColor.None)
            {
                return false;
            }

            StoneColor placedColor = CurrentTurn;
            board[x, y] = placedColor;
            LastMoveX = x;
            LastMoveY = y;

            if (HasFiveFrom(x, y, placedColor))
            {
                Winner = placedColor;
            }
            else
            {
                CurrentTurn = placedColor == StoneColor.Black
                    ? StoneColor.White
                    : StoneColor.Black;
            }

            return true;
        }

        public void Restart()
        {
            Array.Clear(board, 0, board.Length);
            CurrentTurn = StoneColor.Black;
            Winner = StoneColor.None;
            LastMoveX = -1;
            LastMoveY = -1;
        }

        private bool HasFiveFrom(int x, int y, StoneColor color)
        {
            foreach ((int directionX, int directionY) in Directions)
            {
                int connected = 1
                    + CountDirection(x, y, directionX, directionY, color)
                    + CountDirection(x, y, -directionX, -directionY, color);

                if (connected >= StonesToWin)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountDirection(int startX, int startY, int stepX, int stepY, StoneColor color)
        {
            int count = 0;
            int x = startX + stepX;
            int y = startY + stepY;

            while (IsInsideBoard(x, y) && board[x, y] == color)
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
