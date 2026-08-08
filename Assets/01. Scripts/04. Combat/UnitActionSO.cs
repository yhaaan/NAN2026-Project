using System;
using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public enum UnitActionKind
    {
        Damage,
        Heal
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
        [SerializeField] private string displayName = "기본 공격";
        [SerializeField] private string powerLabel = "공격력";

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

        public static CombatActionPlan BuildBasicAttack(
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

            return target == null
                ? new CombatActionPlan(UnitActionKind.Damage, EmptyEffects)
                : new CombatActionPlan(
                    UnitActionKind.Damage,
                    new[] { new CombatEffect(target, CombatEffectKind.Damage, actor.Definition.Power) });
        }

        public static CombatActionPlan BuildAreaHeal(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            var effects = new List<CombatEffect>();
            foreach (BoardUnit candidate in units)
            {
                if (candidate.IsAlive
                    && candidate.Side == actor.Side
                    && candidate.CurrentHealth < candidate.Definition.MaxHealth
                    && actor.DistanceTo(candidate) <= actor.Definition.Range)
                {
                    effects.Add(new CombatEffect(
                        candidate,
                        CombatEffectKind.Heal,
                        actor.Definition.Power));
                }
            }

            return new CombatActionPlan(UnitActionKind.Heal, effects);
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
            bool candidateTank = candidate.Definition.Role == UnitRole.Tank;
            bool currentTank = current.Definition.Role == UnitRole.Tank;
            if (candidateTank != currentTank)
            {
                return candidateTank;
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
