using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    internal sealed class GomokuComTacticalEvaluator
    {
        private static readonly (int x, int y)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        private readonly GomokuGame game;
        private readonly StoneColor side;
        private readonly float combatDuration;
        private readonly List<ThreatWindow> opponentThreats;

        public GomokuComTacticalEvaluator(
            GomokuGame game,
            StoneColor side,
            float combatDuration)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.side = side;
            this.combatDuration = Math.Max(0.1f, combatDuration);
            opponentThreats = FindThreatWindows(game, GomokuGame.OpponentOf(side));
        }

        public float Evaluate(UnitDefinitionSO definition, int x, int y)
        {
            var candidate = new BoardUnit(
                definition,
                side,
                x,
                y,
                int.MaxValue);
            var simulatedUnits = new List<BoardUnit>(game.Units.Count + 1);
            simulatedUnits.AddRange(game.Units);
            simulatedUnits.Add(candidate);

            float actionInterval = EstimateActionInterval(candidate, simulatedUnits);
            float incomingBeforeFirstAction = EstimateIncomingDamage(
                candidate,
                simulatedUnits,
                actionInterval);
            float incomingDuringCombat = EstimateIncomingDamage(
                candidate,
                simulatedUnits,
                combatDuration);
            float incomingDps = incomingDuringCombat / combatDuration;
            float survivalTime = incomingDps <= 0f
                ? combatDuration
                : Math.Min(combatDuration, definition.MaxHealth / incomingDps);
            bool diesBeforeFirstAction = incomingBeforeFirstAction >= definition.MaxHealth;
            bool expectedToDie = incomingDuringCombat >= definition.MaxHealth;
            float actionHorizon = expectedToDie
                ? Math.Max(0f, survivalTime - 0.001f)
                : combatDuration + 0.001f;
            int projectedActions = diesBeforeFirstAction
                ? 0
                : Math.Max(0, (int)Math.Floor(actionHorizon / actionInterval));

            return EvaluateSacrificeRisk(
                    definition,
                    x,
                    y,
                    incomingDps,
                    diesBeforeFirstAction,
                    expectedToDie)
                + EvaluateThreatBreakingPower(
                    candidate,
                    simulatedUnits,
                    projectedActions,
                    expectedToDie);
        }

        private float EvaluateSacrificeRisk(
            UnitDefinitionSO definition,
            int x,
            int y,
            float incomingDps,
            bool diesBeforeFirstAction,
            bool expectedToDie)
        {
            float roleRiskMultiplier = GetRoleRiskMultiplier(definition);
            float score = 0f;

            if (diesBeforeFirstAction)
            {
                score -= (4_000f + (int)definition.Grade * 1_500f) * roleRiskMultiplier;
            }
            else if (expectedToDie)
            {
                score -= EstimateUnitValue(definition) * roleRiskMultiplier;
            }

            int adjacentEnemies = 0;
            foreach (BoardUnit unit in game.Units)
            {
                if (unit.Side != side
                    && Math.Max(Math.Abs(x - unit.X), Math.Abs(y - unit.Y)) <= 1)
                {
                    adjacentEnemies++;
                }
            }

            if (adjacentEnemies > 0 && IsBackLineRole(definition))
            {
                score -= adjacentEnemies * (1_200f + (int)definition.Grade * 900f);
            }

            if (diesBeforeFirstAction)
            {
                foreach (ThreatWindow threat in opponentThreats)
                {
                    if (threat.Contains(x, y))
                    {
                        score -= DirectBlockSacrificePenalty(threat.StoneCount)
                            * roleRiskMultiplier;
                    }
                }
            }

            if (definition.Role == UnitRole.Guardian && incomingDps > 0f)
            {
                score += Math.Min(incomingDps, definition.MaxHealth) * 2f;
            }

            return score;
        }

        private float EvaluateThreatBreakingPower(
            BoardUnit candidate,
            IReadOnlyList<BoardUnit> simulatedUnits,
            int projectedActions,
            bool expectedToDie)
        {
            if (opponentThreats.Count == 0)
            {
                return 0f;
            }

            CombatActionPlan plan = projectedActions > 0
                ? CombatActionRules.BuildAbilityPlan(
                    candidate,
                    simulatedUnits,
                    EstimateModifiedPower(candidate, simulatedUnits))
                : null;
            float score = 0f;

            foreach (ThreatWindow threat in opponentThreats)
            {
                if (threat.Contains(candidate.X, candidate.Y))
                {
                    continue;
                }

                float bestDamageRatio = 0f;
                bool breaksThreat = false;
                if (plan != null)
                {
                    foreach (CombatEffect effect in plan.Effects)
                    {
                        if (effect.Kind != CombatEffectKind.Damage
                            || !threat.Contains(effect.Target))
                        {
                            continue;
                        }

                        int projectedDamage = ApplyEstimatedMitigation(
                                effect.Target,
                                effect.Amount)
                            * projectedActions;
                        int effectiveHealth = EstimateEffectiveHealth(effect.Target);
                        bestDamageRatio = Math.Max(
                            bestDamageRatio,
                            Math.Min(1f, (float)projectedDamage / effectiveHealth));
                        breaksThreat |= projectedDamage >= effectiveHealth;
                    }
                }

                if (!breaksThreat
                    && expectedToDie
                    && candidate.Definition.Ability == UnitAbility.DeathExplosion)
                {
                    foreach (BoardUnit threatUnit in threat.Units)
                    {
                        if (candidate.DistanceTo(threatUnit) > 1)
                        {
                            continue;
                        }

                        int explosionDamage = ApplyEstimatedMitigation(
                            threatUnit,
                            candidate.Definition.AbilityPower);
                        breaksThreat |= explosionDamage >= EstimateEffectiveHealth(threatUnit);
                    }
                }

                float threatValue = ThreatDefenseValue(
                    threat.StoneCount,
                    candidate.Definition);
                if (breaksThreat)
                {
                    score += threatValue;
                }
                else if (threat.StoneCount < 4)
                {
                    score += threatValue * bestDamageRatio * 0.2f;
                }
            }

            return score;
        }

        private static float EstimateIncomingDamage(
            BoardUnit candidate,
            IReadOnlyList<BoardUnit> simulatedUnits,
            float duration)
        {
            float damage = 0f;
            foreach (BoardUnit enemy in simulatedUnits)
            {
                if (enemy == candidate || enemy.Side == candidate.Side || !enemy.IsAlive)
                {
                    continue;
                }

                float interval = EstimateActionInterval(enemy, simulatedUnits);
                int actions = Math.Max(
                    0,
                    (int)Math.Floor((duration + 0.001f) / interval));
                if (actions == 0)
                {
                    continue;
                }

                CombatActionPlan plan = CombatActionRules.BuildAbilityPlan(
                    enemy,
                    simulatedUnits,
                    EstimateModifiedPower(enemy, simulatedUnits));
                foreach (CombatEffect effect in plan.Effects)
                {
                    if (effect.Kind == CombatEffectKind.Damage && effect.Target == candidate)
                    {
                        damage += ApplyEstimatedMitigation(candidate, effect.Amount) * actions;
                    }
                }
            }

            return damage;
        }

        private static int EstimateModifiedPower(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            int power = actor.Definition.Power;
            if (actor.Definition.Ability == UnitAbility.IsolatedAssault)
            {
                bool hasAdjacentAlly = false;
                foreach (BoardUnit unit in units)
                {
                    if (unit != actor
                        && unit.IsAlive
                        && unit.Side == actor.Side
                        && actor.DistanceTo(unit) <= 1)
                    {
                        hasAdjacentAlly = true;
                        break;
                    }
                }

                power = hasAdjacentAlly
                    ? actor.Definition.Power
                    : actor.Definition.AbilityPower;
            }

            float weakenRatio = 0f;
            foreach (BoardUnit enemy in units)
            {
                if (enemy.IsAlive
                    && enemy.Side != actor.Side
                    && enemy.Definition.Ability == UnitAbility.WeakenAura
                    && enemy.DistanceTo(actor) <= enemy.Definition.Range)
                {
                    weakenRatio = Math.Max(weakenRatio, enemy.Definition.AbilityRatio);
                }
            }

            return Math.Max(0, (int)Math.Round(power * (1f - weakenRatio)));
        }

        private static float EstimateActionInterval(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            float speedBonus = 0f;
            if (!actor.Definition.IsSupport)
            {
                foreach (BoardUnit ally in units)
                {
                    if (ally != actor
                        && ally.IsAlive
                        && ally.Side == actor.Side
                        && ally.Definition.Ability == UnitAbility.HasteAura
                        && ally.DistanceTo(actor) <= ally.Definition.Range)
                    {
                        speedBonus = Math.Max(speedBonus, ally.Definition.AbilityRatio);
                    }
                }
            }

            return Math.Max(0.1f, actor.Definition.ActionInterval / (1f + speedBonus));
        }

        private static int ApplyEstimatedMitigation(BoardUnit target, int rawDamage)
        {
            if (target.Definition.Ability != UnitAbility.DamageReduction)
            {
                return rawDamage;
            }

            return Math.Max(
                1,
                (int)Math.Round(rawDamage * (1f - target.Definition.AbilityRatio)));
        }

        private static int EstimateEffectiveHealth(BoardUnit unit)
        {
            int health = unit.CurrentHealth;
            if (unit.Definition.Ability == UnitAbility.PhoenixRebirth
                && !unit.LifetimeAbilityUsed)
            {
                health += Math.Max(
                    1,
                    (int)Math.Round(unit.Definition.MaxHealth * unit.Definition.AbilityRatio));
            }

            return Math.Max(1, health);
        }

        private static float EstimateUnitValue(UnitDefinitionSO definition)
        {
            float actionsPerSecond = definition.Power
                / Math.Max(0.1f, definition.ActionInterval);
            float actionValue = definition.IsSupport
                ? actionsPerSecond * 0.7f
                : actionsPerSecond;
            return definition.MaxHealth * 1.5f
                + actionValue * 20f
                + (int)definition.Grade * 500f;
        }

        private static float GetRoleRiskMultiplier(UnitDefinitionSO definition)
        {
            if (definition.Ability == UnitAbility.DeathExplosion)
            {
                return 0.35f;
            }

            switch (definition.Role)
            {
                case UnitRole.Guardian: return 0.35f;
                case UnitRole.Vanguard: return 0.75f;
                case UnitRole.Marksman:
                case UnitRole.Caster: return 1.45f;
                default: return 1.25f;
            }
        }

        private static bool IsBackLineRole(UnitDefinitionSO definition)
        {
            return definition.Role == UnitRole.Marksman
                || definition.Role == UnitRole.Caster
                || definition.Role == UnitRole.Support;
        }

        private static float ThreatDefenseValue(
            int stones,
            UnitDefinitionSO definition)
        {
            float reliability = ThreatBreakReliability(definition);
            switch (stones)
            {
                case 3: return 2_800f;
                case 4: return 1_050_000f * reliability;
                case 5: return 2_000_000f * reliability;
                default: return 0f;
            }
        }

        private static float ThreatBreakReliability(UnitDefinitionSO definition)
        {
            switch (definition.Ability)
            {
                case UnitAbility.PiercingShot:
                    return 1f;
                case UnitAbility.DeathExplosion:
                    return 0.9f;
                case UnitAbility.Meteor:
                case UnitAbility.ChainLightning:
                    return 0.7f;
                default:
                    return 0.35f;
            }
        }

        private static float DirectBlockSacrificePenalty(int stones)
        {
            switch (stones)
            {
                case 3: return 25_000f;
                case 4: return 60_000f;
                default: return 0f;
            }
        }

        private static List<ThreatWindow> FindThreatWindows(
            GomokuGame game,
            StoneColor side)
        {
            var threats = new List<ThreatWindow>();
            StoneColor blockingSide = GomokuGame.OpponentOf(side);

            foreach ((int directionX, int directionY) in Directions)
            {
                for (int startX = 0; startX < GomokuGame.BoardSize; startX++)
                {
                    for (int startY = 0; startY < GomokuGame.BoardSize; startY++)
                    {
                        int endX = startX + 4 * directionX;
                        int endY = startY + 4 * directionY;
                        if (!IsInsideBoard(endX, endY))
                        {
                            continue;
                        }

                        var units = new List<BoardUnit>(5);
                        bool blocked = false;
                        for (int offset = 0; offset < 5; offset++)
                        {
                            BoardUnit unit = game.GetUnit(
                                startX + offset * directionX,
                                startY + offset * directionY);
                            if (unit == null)
                            {
                                continue;
                            }

                            if (unit.Side == blockingSide)
                            {
                                blocked = true;
                                break;
                            }

                            if (unit.Side == side)
                            {
                                units.Add(unit);
                            }
                        }

                        if (!blocked && units.Count >= 3)
                        {
                            threats.Add(new ThreatWindow(
                                startX,
                                startY,
                                directionX,
                                directionY,
                                units));
                        }
                    }
                }
            }

            return threats;
        }

        private static bool IsInsideBoard(int x, int y)
        {
            return x >= 0
                && x < GomokuGame.BoardSize
                && y >= 0
                && y < GomokuGame.BoardSize;
        }

        private sealed class ThreatWindow
        {
            private readonly int startX;
            private readonly int startY;
            private readonly int directionX;
            private readonly int directionY;
            private readonly IReadOnlyList<BoardUnit> units;

            public int StoneCount => units.Count;
            public IReadOnlyList<BoardUnit> Units => units;

            public ThreatWindow(
                int startX,
                int startY,
                int directionX,
                int directionY,
                IReadOnlyList<BoardUnit> units)
            {
                this.startX = startX;
                this.startY = startY;
                this.directionX = directionX;
                this.directionY = directionY;
                this.units = units;
            }

            public bool Contains(int x, int y)
            {
                for (int offset = 0; offset < 5; offset++)
                {
                    if (x == startX + offset * directionX
                        && y == startY + offset * directionY)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool Contains(BoardUnit unit)
            {
                for (int index = 0; index < units.Count; index++)
                {
                    if (units[index] == unit)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
