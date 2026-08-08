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
        private static readonly (int x, int y)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

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
            StoneColor opponent = GomokuGame.OpponentOf(side);
            if (game.WouldWin(x, y, side))
            {
                return 1_000_000f;
            }

            if (game.WouldWin(x, y, opponent))
            {
                return 900_000f;
            }

            float score = 10f - Math.Max(Math.Abs(x - 7), Math.Abs(y - 7)) * 0.6f;
            score += EvaluateFiveWindows(game, x, y, side);
            score += EvaluateFiveWindows(game, x, y, opponent) * 0.9f;
            score += EvaluateContiguousLines(game, x, y, side);
            score += EvaluateContiguousLines(game, x, y, opponent) * 0.85f;
            return score;
        }

        private static float EvaluateFiveWindows(
            GomokuGame game,
            int candidateX,
            int candidateY,
            StoneColor evaluatedSide)
        {
            float score = 0f;
            StoneColor blockingSide = GomokuGame.OpponentOf(evaluatedSide);

            foreach ((int directionX, int directionY) in Directions)
            {
                for (int candidateOffset = 0; candidateOffset < 5; candidateOffset++)
                {
                    int startX = candidateX - candidateOffset * directionX;
                    int startY = candidateY - candidateOffset * directionY;
                    int stones = 1;
                    float healthRatioSum = 1f;
                    bool blocked = false;

                    for (int offset = 0; offset < 5; offset++)
                    {
                        int x = startX + offset * directionX;
                        int y = startY + offset * directionY;
                        if (!IsInsideBoard(x, y))
                        {
                            blocked = true;
                            break;
                        }

                        if (x == candidateX && y == candidateY)
                        {
                            continue;
                        }

                        BoardUnit unit = game.GetUnit(x, y);
                        if (unit == null)
                        {
                            continue;
                        }

                        if (unit.Side == blockingSide)
                        {
                            blocked = true;
                            break;
                        }

                        stones++;
                        healthRatioSum += (float)unit.CurrentHealth / unit.Definition.MaxHealth;
                    }

                    if (blocked || stones < 2)
                    {
                        continue;
                    }

                    int openEnds = 0;
                    if (IsEmpty(game, startX - directionX, startY - directionY))
                    {
                        openEnds++;
                    }

                    if (IsEmpty(game, startX + 5 * directionX, startY + 5 * directionY))
                    {
                        openEnds++;
                    }

                    float shapeScore = ScoreForStoneCount(stones);
                    float opennessMultiplier = openEnds == 2 ? 1.35f : openEnds == 1 ? 1.12f : 1f;
                    float durabilityMultiplier = 0.6f + 0.4f * (healthRatioSum / stones);
                    score += shapeScore * opennessMultiplier * durabilityMultiplier;
                }
            }

            return score;
        }

        private static float EvaluateContiguousLines(
            GomokuGame game,
            int x,
            int y,
            StoneColor side)
        {
            float score = 0f;
            foreach ((int directionX, int directionY) in Directions)
            {
                int negative = CountDirection(game, x, y, -directionX, -directionY, side);
                int positive = CountDirection(game, x, y, directionX, directionY, side);
                int connected = 1 + negative + positive;
                if (connected < 2)
                {
                    continue;
                }

                int openEnds = 0;
                if (IsEmpty(
                    game,
                    x - (negative + 1) * directionX,
                    y - (negative + 1) * directionY))
                {
                    openEnds++;
                }

                if (IsEmpty(
                    game,
                    x + (positive + 1) * directionX,
                    y + (positive + 1) * directionY))
                {
                    openEnds++;
                }

                float opennessMultiplier = openEnds == 2 ? 1.5f : openEnds == 1 ? 1.15f : 0.8f;
                score += ScoreForStoneCount(Math.Min(connected, 4)) * opennessMultiplier;
            }

            return score;
        }

        private static float ScoreForStoneCount(int stones)
        {
            switch (stones)
            {
                case 2: return 90f;
                case 3: return 1_600f;
                case 4: return 30_000f;
                case 5: return 150_000f;
                default: return 0f;
            }
        }

        private static float EvaluateUnitPosition(
            GomokuGame game,
            UnitDefinitionSO definition,
            int x,
            int y,
            StoneColor side)
        {
            float interval = Math.Max(0.1f, definition.ActionInterval);
            float actionPower = definition.IsSupport ? 0f : definition.Power / interval;
            float incomingPower = 0f;
            float outgoingOpportunity = 0f;
            float healingOpportunity = 0f;
            float alliedHealingSupport = 0f;
            int nearbyAllies = 0;
            int adjacentAllies = 0;
            int adjacentEnemies = 0;

            foreach (BoardUnit unit in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y));
                if (unit.Side == side)
                {
                    if (distance <= Math.Max(1, definition.Range)) nearbyAllies++;
                    if (distance <= 1) adjacentAllies++;

                    if (definition.IsHealer && distance <= definition.Range)
                    {
                        int missingHealth = unit.Definition.MaxHealth - unit.CurrentHealth;
                        healingOpportunity += Math.Min(definition.Power, missingHealth) / interval;
                    }

                    if (unit.Definition.IsHealer && distance <= unit.Definition.Range)
                    {
                        alliedHealingSupport += unit.Definition.Power
                            / Math.Max(0.1f, unit.Definition.ActionInterval);
                    }

                    continue;
                }

                if (distance <= 1) adjacentEnemies++;
                if (!definition.IsSupport && distance <= definition.Range)
                {
                    float targetImportance = 1f + EvaluateExistingConnection(game, unit) * 0.3f;
                    if (unit.CurrentHealth <= definition.Power) targetImportance += 0.5f;
                    outgoingOpportunity += actionPower * targetImportance;
                }

                if (!unit.Definition.IsSupport && distance <= unit.Definition.Range)
                {
                    incomingPower += unit.Definition.Power
                        / Math.Max(0.1f, unit.Definition.ActionInterval);
                }
            }

            float score = definition.MaxHealth * 0.12f + actionPower * 1.5f;
            score += outgoingOpportunity * 2.2f;
            score += healingOpportunity * 2f;
            score += alliedHealingSupport * 0.8f;
            score += (int)definition.Grade * 18f;

            if (incomingPower > 0f)
            {
                score += definition.MaxHealth / incomingPower * 24f;
                score -= incomingPower * 0.35f;
            }

            switch (definition.Ability)
            {
                case UnitAbility.DeathExplosion:
                    score += adjacentEnemies * definition.AbilityPower * 2f;
                    score -= adjacentAllies * definition.AbilityPower * 2.4f;
                    break;
                case UnitAbility.IsolatedAssault:
                    score += adjacentAllies == 0 ? definition.AbilityPower * 7f : -adjacentAllies * 240f;
                    break;
                case UnitAbility.DamageReduction:
                    score += incomingPower * definition.AbilityRatio * 2.5f;
                    break;
                case UnitAbility.PiercingShot:
                    score += BestRayEnemyCount(game, x, y, definition.Range, side) * definition.Power * 4f;
                    break;
                case UnitAbility.WeakenAura:
                    score += EnemyPowerInRange(game, x, y, definition.Range, side)
                        * definition.AbilityRatio * 3f;
                    break;
                case UnitAbility.HasteAura:
                    score += AllyPowerInRange(game, x, y, definition.Range, side)
                        * definition.AbilityRatio * 3f;
                    break;
                case UnitAbility.Meteor:
                    score += BestEnemyCluster(game, x, y, definition.Range, side) * definition.Power * 3f;
                    break;
                case UnitAbility.DamageRedirect:
                    score += adjacentAllies * 32f;
                    break;
                case UnitAbility.PhoenixRebirth:
                    score += definition.MaxHealth * definition.AbilityRatio * 0.25f;
                    break;
                case UnitAbility.ChainLightning:
                    score += Math.Min(3, CountEnemiesInRange(game, x, y, definition.Range, side))
                        * definition.Power * 2.5f;
                    break;
                case UnitAbility.SaintProtection:
                    score += nearbyAllies * 28f;
                    break;
            }

            if (definition.Role == UnitRole.Guardian) score += nearbyAllies * 9f;
            else if (definition.IsHealer) score += nearbyAllies * 13f;
            return score;
        }

        private static int BestRayEnemyCount(
            GomokuGame game, int x, int y, int range, StoneColor side)
        {
            int best = 0;
            foreach ((int stepX, int stepY) in new[]
            {
                (1, 0), (-1, 0), (0, 1), (0, -1),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            })
            {
                int count = 0;
                for (int distance = 1; distance <= range; distance++)
                {
                    int targetX = x + stepX * distance;
                    int targetY = y + stepY * distance;
                    if (!IsInsideBoard(targetX, targetY)) break;
                    StoneColor stone = game.GetStone(targetX, targetY);
                    if (stone != StoneColor.None && stone != side) count++;
                }
                best = Math.Max(best, count);
            }
            return best;
        }

        private static float EnemyPowerInRange(
            GomokuGame game, int x, int y, int range, StoneColor side)
        {
            float power = 0f;
            foreach (BoardUnit unit in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y));
                if (unit.Side != side && !unit.Definition.IsSupport && distance <= range)
                {
                    power += unit.Definition.Power / Math.Max(0.1f, unit.Definition.ActionInterval);
                }
            }
            return power;
        }

        private static float AllyPowerInRange(
            GomokuGame game, int x, int y, int range, StoneColor side)
        {
            float power = 0f;
            foreach (BoardUnit unit in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y));
                if (unit.Side == side && !unit.Definition.IsSupport && distance <= range)
                {
                    power += unit.Definition.Power / Math.Max(0.1f, unit.Definition.ActionInterval);
                }
            }
            return power;
        }

        private static int BestEnemyCluster(
            GomokuGame game, int x, int y, int range, StoneColor side)
        {
            int best = 0;
            foreach (BoardUnit target in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - target.X), Math.Abs(y - target.Y));
                if (target.Side == side || distance > range) continue;
                int cluster = 0;
                foreach (BoardUnit candidate in game.Units)
                {
                    if (candidate.Side != side && target.DistanceTo(candidate) <= 1) cluster++;
                }
                best = Math.Max(best, cluster);
            }
            return best;
        }

        private static int CountEnemiesInRange(
            GomokuGame game, int x, int y, int range, StoneColor side)
        {
            int count = 0;
            foreach (BoardUnit unit in game.Units)
            {
                int distance = Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y));
                if (unit.Side != side && distance <= range) count++;
            }
            return count;
        }

        private static int EvaluateExistingConnection(GomokuGame game, BoardUnit unit)
        {
            int best = 1;
            foreach ((int directionX, int directionY) in Directions)
            {
                int connected = 1
                    + CountDirection(game, unit.X, unit.Y, directionX, directionY, unit.Side)
                    + CountDirection(game, unit.X, unit.Y, -directionX, -directionY, unit.Side);
                best = Math.Max(best, connected);
            }

            return best;
        }

        private static int CountDirection(
            GomokuGame game,
            int startX,
            int startY,
            int stepX,
            int stepY,
            StoneColor side)
        {
            int count = 0;
            int x = startX + stepX;
            int y = startY + stepY;
            while (IsInsideBoard(x, y) && game.GetStone(x, y) == side)
            {
                count++;
                x += stepX;
                y += stepY;
            }

            return count;
        }

        private static bool IsEmpty(GomokuGame game, int x, int y)
        {
            return IsInsideBoard(x, y) && game.GetStone(x, y) == StoneColor.None;
        }

        private static bool IsInsideBoard(int x, int y)
        {
            return x >= 0 && x < GomokuGame.BoardSize && y >= 0 && y < GomokuGame.BoardSize;
        }
    }
}