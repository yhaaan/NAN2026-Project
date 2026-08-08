using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public enum UnitActionKind
    {
        Damage,
        Heal,
        Support
    }

    public enum CombatEffectKind
    {
        Damage,
        Heal
    }

    public readonly struct CombatEffect
    {
        public BoardUnit Target { get; }
        public CombatEffectKind Kind { get; }
        public int Amount { get; }

        public CombatEffect(BoardUnit target, CombatEffectKind kind, int amount)
        {
            Target = target;
            Kind = kind;
            Amount = Mathf.Max(0, amount);
        }
    }

    public sealed class CombatActionPlan
    {
        private static readonly CombatEffect[] EmptyEffects = Array.Empty<CombatEffect>();

        public UnitActionKind Kind { get; }
        public IReadOnlyList<CombatEffect> Effects { get; }

        public CombatActionPlan(UnitActionKind kind, IReadOnlyList<CombatEffect> effects)
        {
            Kind = kind;
            Effects = effects ?? EmptyEffects;
        }
    }

    public abstract class UnitActionSO : ScriptableObject
    {
        [SerializeField] private string displayName = "기본 행동";
        [SerializeField] private string powerLabel = "위력";

        public abstract UnitActionKind Kind { get; }
        public string DisplayName => displayName;
        public string PowerLabel => powerLabel;

        public abstract CombatActionPlan BuildPlan(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units);
    }

    internal static class CombatActionRules
    {
        private static readonly CombatEffect[] EmptyEffects = Array.Empty<CombatEffect>();
        private static readonly (int x, int y)[] Rays =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        public static CombatActionPlan BuildAbilityPlan(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            switch (actor.Definition.Ability)
            {
                case UnitAbility.AreaHeal:
                case UnitAbility.SaintProtection:
                    return BuildAreaHeal(actor, units, power);
                case UnitAbility.LowestHealthHeal:
                    return BuildLowestHealthHeal(actor, units, power);
                case UnitAbility.PiercingShot:
                    return BuildPiercingShot(actor, units, power);
                case UnitAbility.Meteor:
                    return BuildMeteor(actor, units, power);
                case UnitAbility.ChainLightning:
                    return BuildChainLightning(actor, units, power);
                case UnitAbility.WeakenAura:
                case UnitAbility.HasteAura:
                    return new CombatActionPlan(UnitActionKind.Support, EmptyEffects);
                default:
                    return actor.Definition.IsSupport
                        ? new CombatActionPlan(UnitActionKind.Support, EmptyEffects)
                        : BuildBasicAttack(actor, units, power);
            }
        }

        public static CombatActionPlan BuildBasicAttack(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            return BuildBasicAttack(actor, units, actor.Definition.Power);
        }

        public static CombatActionPlan BuildBasicAttack(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            BoardUnit target = FindPreferredEnemy(actor, units);
            return target == null
                ? new CombatActionPlan(UnitActionKind.Damage, EmptyEffects)
                : new CombatActionPlan(
                    UnitActionKind.Damage,
                    new[] { new CombatEffect(target, CombatEffectKind.Damage, power) });
        }

        public static CombatActionPlan BuildAreaHeal(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            return BuildAreaHeal(actor, units, actor.Definition.Power);
        }

        private static CombatActionPlan BuildAreaHeal(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            var effects = new List<CombatEffect>();
            foreach (BoardUnit candidate in units)
            {
                if (candidate.IsAlive
                    && candidate.Side == actor.Side
                    && candidate.CurrentHealth < candidate.Definition.MaxHealth
                    && actor.DistanceTo(candidate) <= actor.Definition.Range)
                {
                    effects.Add(new CombatEffect(candidate, CombatEffectKind.Heal, power));
                }
            }

            return new CombatActionPlan(UnitActionKind.Heal, effects);
        }

        private static CombatActionPlan BuildLowestHealthHeal(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            BoardUnit target = units
                .Where(candidate => candidate.IsAlive
                    && candidate.Side == actor.Side
                    && candidate.CurrentHealth < candidate.Definition.MaxHealth
                    && actor.DistanceTo(candidate) <= actor.Definition.Range)
                .OrderBy(candidate => (float)candidate.CurrentHealth / candidate.Definition.MaxHealth)
                .ThenBy(candidate => candidate.CurrentHealth)
                .ThenBy(candidate => candidate.PlacementOrder)
                .FirstOrDefault();

            return target == null
                ? new CombatActionPlan(UnitActionKind.Heal, EmptyEffects)
                : new CombatActionPlan(
                    UnitActionKind.Heal,
                    new[] { new CombatEffect(target, CombatEffectKind.Heal, power) });
        }

        private static CombatActionPlan BuildPiercingShot(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            List<BoardUnit> bestTargets = null;
            foreach ((int stepX, int stepY) in Rays)
            {
                var targets = new List<BoardUnit>();
                for (int distance = 1; distance <= actor.Definition.Range; distance++)
                {
                    int x = actor.X + stepX * distance;
                    int y = actor.Y + stepY * distance;
                    BoardUnit candidate = units.FirstOrDefault(unit => unit.IsAlive && unit.X == x && unit.Y == y);
                    if (candidate != null && candidate.Side != actor.Side)
                    {
                        targets.Add(candidate);
                    }
                }

                if (bestTargets == null || targets.Count > bestTargets.Count)
                {
                    bestTargets = targets;
                }
            }

            if (bestTargets == null || bestTargets.Count == 0)
            {
                return new CombatActionPlan(UnitActionKind.Damage, EmptyEffects);
            }

            return new CombatActionPlan(
                UnitActionKind.Damage,
                bestTargets.Select(target => new CombatEffect(target, CombatEffectKind.Damage, power)).ToArray());
        }

        private static CombatActionPlan BuildMeteor(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            BoardUnit target = FindPreferredEnemy(actor, units);
            if (target == null)
            {
                return new CombatActionPlan(UnitActionKind.Damage, EmptyEffects);
            }

            CombatEffect[] effects = units
                .Where(candidate => candidate.IsAlive
                    && candidate.Side != actor.Side
                    && candidate.DistanceTo(target) <= 1)
                .OrderBy(candidate => candidate.PlacementOrder)
                .Select(candidate => new CombatEffect(candidate, CombatEffectKind.Damage, power))
                .ToArray();
            return new CombatActionPlan(UnitActionKind.Damage, effects);
        }

        private static CombatActionPlan BuildChainLightning(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units,
            int power)
        {
            BoardUnit target = FindPreferredEnemy(actor, units);
            if (target == null)
            {
                return new CombatActionPlan(UnitActionKind.Damage, EmptyEffects);
            }

            var effects = new List<CombatEffect>
            {
                new CombatEffect(target, CombatEffectKind.Damage, power)
            };
            BoardUnit current = target;
            float[] ratios = { 0.7f, 0.45f };
            foreach (float ratio in ratios)
            {
                BoardUnit next = units
                    .Where(candidate => candidate.IsAlive
                        && candidate.Side != actor.Side
                        && candidate != target
                        && effects.All(effect => effect.Target != candidate)
                        && current.DistanceTo(candidate) <= 2)
                    .OrderBy(candidate => current.DistanceTo(candidate))
                    .ThenBy(candidate => candidate.PlacementOrder)
                    .FirstOrDefault();
                if (next == null)
                {
                    break;
                }

                effects.Add(new CombatEffect(
                    next,
                    CombatEffectKind.Damage,
                    Mathf.RoundToInt(power * ratio)));
                current = next;
            }

            return new CombatActionPlan(UnitActionKind.Damage, effects);
        }

        private static BoardUnit FindPreferredEnemy(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            BoardUnit target = null;
            foreach (BoardUnit candidate in units)
            {
                if (!IsAttackCandidate(actor, candidate))
                {
                    continue;
                }

                if (target == null || IsPreferredAttackTarget(actor, candidate, target))
                {
                    target = candidate;
                }
            }

            return target;
        }

        private static bool IsAttackCandidate(BoardUnit actor, BoardUnit candidate)
        {
            return candidate.IsAlive
                && candidate.Side != actor.Side
                && actor.DistanceTo(candidate) <= actor.Definition.Range;
        }

        private static bool IsPreferredAttackTarget(
            BoardUnit actor,
            BoardUnit candidate,
            BoardUnit current)
        {
            bool candidateGuardian = candidate.Definition.Role == UnitRole.Guardian;
            bool currentGuardian = current.Definition.Role == UnitRole.Guardian;
            if (candidateGuardian != currentGuardian)
            {
                return candidateGuardian;
            }

            int candidateDistance = actor.DistanceTo(candidate);
            int currentDistance = actor.DistanceTo(current);
            if (candidateDistance != currentDistance)
            {
                return candidateDistance < currentDistance;
            }

            if (candidate.CurrentHealth != current.CurrentHealth)
            {
                return candidate.CurrentHealth < current.CurrentHealth;
            }

            return candidate.PlacementOrder < current.PlacementOrder;
        }
    }
}