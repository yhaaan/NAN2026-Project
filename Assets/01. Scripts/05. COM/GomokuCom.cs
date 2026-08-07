using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    public readonly struct ComDecision
    {
        public int X { get; }
        public int Y { get; }
        public int OfferIndex { get; }
        public float Score { get; }

        public ComDecision(int x, int y, int offerIndex, float score)
        {
            X = x;
            Y = y;
            OfferIndex = offerIndex;
            Score = score;
        }
    }

    public sealed class GomokuCom
    {
        private readonly Random random;

        public GomokuCom(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public ComDecision ChooseMove(
            GomokuGame game,
            IReadOnlyList<UnitDefinitionSO> offers,
            StoneColor side)
        {
            ComDecision best = new ComDecision(-1, -1, -1, float.MinValue);

            for (int x = 0; x < GomokuGame.BoardSize; x++)
            {
                for (int y = 0; y < GomokuGame.BoardSize; y++)
                {
                    if (game.GetStone(x, y) != StoneColor.None)
                    {
                        continue;
                    }

                    float boardScore = EvaluateBoardPosition(game, x, y, side);
                    for (int offerIndex = 0; offerIndex < offers.Count; offerIndex++)
                    {
                        float score = boardScore
                            + EvaluateUnitPosition(game, offers[offerIndex], x, y, side)
                            + (float)random.NextDouble() * 0.25f;

                        if (score > best.Score)
                        {
                            best = new ComDecision(x, y, offerIndex, score);
                        }
                    }
                }
            }

            return best;
        }

        private static float EvaluateBoardPosition(GomokuGame game, int x, int y, StoneColor side)
        {
            if (game.WouldWin(x, y, side))
            {
                return 1_000_000f;
            }

            if (game.WouldWin(x, y, GomokuGame.OpponentOf(side)))
            {
                return 500_000f;
            }

            float score = 14f - Math.Max(Math.Abs(x - 7), Math.Abs(y - 7));
            score += CountNeighbours(game, x, y, side, 2) * 36f;
            score += CountNeighbours(game, x, y, GomokuGame.OpponentOf(side), 2) * 28f;
            return score;
        }

        private static float EvaluateUnitPosition(
            GomokuGame game,
            UnitDefinitionSO definition,
            int x,
            int y,
            StoneColor side)
        {
            float score = definition.MaxHealth * 0.03f + definition.Power * 0.5f;
            int allies = 0;
            int enemiesInRange = 0;

            foreach (BoardUnit unit in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y));
                if (unit.Side == side && distance <= Math.Max(1, definition.Range))
                {
                    allies++;
                }
                else if (unit.Side != side && distance <= definition.Range)
                {
                    enemiesInRange++;
                }
            }

            score += enemiesInRange * definition.Power;
            if (definition.Role == UnitRole.Tank)
            {
                score += allies * 12f;
            }
            else if (definition.Role == UnitRole.Healer)
            {
                score += allies * 18f;
            }

            return score;
        }

        private static int CountNeighbours(
            GomokuGame game,
            int centerX,
            int centerY,
            StoneColor side,
            int radius)
        {
            int count = 0;
            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(GomokuGame.BoardSize - 1, centerX + radius); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y <= Math.Min(GomokuGame.BoardSize - 1, centerY + radius); y++)
                {
                    if (game.GetStone(x, y) == side)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
