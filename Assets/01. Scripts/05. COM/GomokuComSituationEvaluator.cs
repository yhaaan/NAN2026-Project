using System;

namespace NAN2026.Gomoku
{
    internal sealed class GomokuComSituationEvaluator
    {
        private readonly GomokuGame game;
        private readonly StoneColor side;
        private readonly float combatDuration;

        public GomokuComSituationEvaluator(
            GomokuGame game,
            StoneColor side,
            float combatDuration)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.side = side;
            this.combatDuration = Math.Max(0.1f, combatDuration);
        }

        public float Evaluate(UnitDefinitionSO definition)
        {
            if (definition == null)
            {
                return float.MinValue;
            }

            int allyCount = 0;
            int enemyCount = 0;
            int woundedAllies = 0;
            int alliedGuardians = 0;
            int alliedHealers = 0;
            int alliedBackLine = 0;
            int enemyBackLine = 0;
            int enemyFrontLine = 0;
            int totalMissingHealth = 0;
            float alliedDamagePerSecond = 0f;
            float enemyDamagePerSecond = 0f;

            foreach (BoardUnit unit in game.Units)
            {
                float damagePerSecond = unit.Definition.IsSupport
                    ? 0f
                    : unit.Definition.Power
                        / Math.Max(0.1f, unit.Definition.ActionInterval);

                if (unit.Side == side)
                {
                    allyCount++;
                    alliedDamagePerSecond += damagePerSecond;
                    if (unit.CurrentHealth < unit.Definition.MaxHealth)
                    {
                        woundedAllies++;
                        totalMissingHealth += unit.Definition.MaxHealth - unit.CurrentHealth;
                    }

                    if (unit.Definition.Role == UnitRole.Guardian) alliedGuardians++;
                    if (unit.Definition.IsHealer) alliedHealers++;
                    if (IsBackLineRole(unit.Definition)) alliedBackLine++;
                }
                else
                {
                    enemyCount++;
                    enemyDamagePerSecond += damagePerSecond;
                    if (IsBackLineRole(unit.Definition)) enemyBackLine++;
                    else enemyFrontLine++;
                }
            }

            int unitDeficit = Math.Max(0, enemyCount - allyCount);
            int largestEnemyCluster = FindLargestEnemyCluster();
            float actionRate = 1f / Math.Max(0.1f, definition.ActionInterval);
            float candidateDamagePerSecond = definition.IsSupport
                ? 0f
                : definition.Power * actionRate;
            float score = 0f;

            if (unitDeficit > 0)
            {
                score += unitDeficit
                    * (definition.MaxHealth * 0.8f + candidateDamagePerSecond * 2f);
            }

            switch (definition.Role)
            {
                case UnitRole.Guardian:
                    score += enemyDamagePerSecond * 2.5f;
                    score += alliedBackLine * 100f;
                    score += unitDeficit * 160f;
                    if (alliedGuardians == 0) score += 280f;
                    break;
                case UnitRole.Vanguard:
                    score += enemyBackLine * 70f;
                    if (alliedGuardians == 0 && alliedBackLine > 0) score += 120f;
                    break;
                case UnitRole.Marksman:
                    score += enemyFrontLine * 55f;
                    score += alliedGuardians > 0 ? 180f : -160f;
                    break;
                case UnitRole.Caster:
                    score += largestEnemyCluster * 95f;
                    score += alliedGuardians > 0 ? 120f : -90f;
                    break;
            }

            if (definition.IsHealer)
            {
                score += EvaluateHealingNeed(
                    definition,
                    woundedAllies,
                    totalMissingHealth,
                    alliedHealers,
                    actionRate);
            }

            switch (definition.Ability)
            {
                case UnitAbility.DeathExplosion:
                    score += largestEnemyCluster * definition.AbilityPower * 0.8f;
                    score += unitDeficit * definition.AbilityPower;
                    break;
                case UnitAbility.IsolatedAssault:
                    score += allyCount <= 2 ? definition.AbilityPower * 2f : 0f;
                    break;
                case UnitAbility.DamageReduction:
                    score += enemyDamagePerSecond * definition.AbilityRatio * 2f;
                    break;
                case UnitAbility.PiercingShot:
                    score += enemyCount * definition.Power * 0.6f;
                    break;
                case UnitAbility.WeakenAura:
                    score += enemyDamagePerSecond * definition.AbilityRatio * 4f;
                    break;
                case UnitAbility.HasteAura:
                    score += alliedDamagePerSecond * definition.AbilityRatio * 4f;
                    break;
                case UnitAbility.Meteor:
                    score += largestEnemyCluster * definition.Power * 1.4f;
                    break;
                case UnitAbility.DamageRedirect:
                    score += alliedBackLine * 120f;
                    score += enemyDamagePerSecond * definition.AbilityRatio * 2f;
                    break;
                case UnitAbility.PhoenixRebirth:
                    score += unitDeficit * definition.MaxHealth * definition.AbilityRatio;
                    break;
                case UnitAbility.ChainLightning:
                    score += Math.Min(3, largestEnemyCluster) * definition.Power * 1.2f;
                    break;
                case UnitAbility.SaintProtection:
                    score += allyCount * 70f;
                    score += woundedAllies * 90f;
                    break;
            }

            return score;
        }

        private float EvaluateHealingNeed(
            UnitDefinitionSO definition,
            int woundedAllies,
            int totalMissingHealth,
            int alliedHealers,
            float actionRate)
        {
            if (woundedAllies == 0)
            {
                return -220f;
            }

            float healingPerSecond = Math.Max(definition.Power, definition.AbilityPower)
                * actionRate;
            int simultaneousTargets = definition.Ability == UnitAbility.AreaHeal
                || definition.Ability == UnitAbility.SaintProtection
                ? woundedAllies
                : 1;
            float usefulHealing = Math.Min(
                totalMissingHealth,
                healingPerSecond * combatDuration * simultaneousTargets);
            float score = totalMissingHealth * 4f
                + usefulHealing * 2f
                + woundedAllies * 140f;
            if (alliedHealers == 0)
            {
                score += 260f;
            }

            return score;
        }

        private int FindLargestEnemyCluster()
        {
            int largest = 0;
            foreach (BoardUnit center in game.Units)
            {
                if (center.Side == side)
                {
                    continue;
                }

                int cluster = 0;
                foreach (BoardUnit candidate in game.Units)
                {
                    if (candidate.Side != side && center.DistanceTo(candidate) <= 1)
                    {
                        cluster++;
                    }
                }

                largest = Math.Max(largest, cluster);
            }

            return largest;
        }

        private static bool IsBackLineRole(UnitDefinitionSO definition)
        {
            return definition.Role == UnitRole.Marksman
                || definition.Role == UnitRole.Caster
                || definition.Role == UnitRole.Support;
        }
    }
}
