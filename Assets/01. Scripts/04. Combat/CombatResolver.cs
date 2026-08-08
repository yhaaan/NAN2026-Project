using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class CombatResolver
    {
        private readonly Dictionary<BoardUnit, float> cooldowns = new Dictionary<BoardUnit, float>();
        private readonly HashSet<BoardUnit> saintProtectionUsed = new HashSet<BoardUnit>();
        private readonly HashSet<BoardUnit> explodedUnits = new HashSet<BoardUnit>();
        private GomokuGame game;

        public float Duration { get; }
        public float Elapsed { get; private set; }
        public string LastAction { get; private set; } = string.Empty;
        public event Action<BoardUnit, BoardUnit, int> UnitDamaged;
        public event Action<BoardUnit, BoardUnit, int> UnitHealed;
        public event Action<CombatActionEvent> ActionResolved;

        public bool IsFinished => game == null
            || Elapsed >= Duration
            || !game.HasAnyUnits(StoneColor.Black)
            || !game.HasAnyUnits(StoneColor.White);

        public CombatResolver(float duration = 10f)
        {
            Duration = duration;
        }

        public void Begin(GomokuGame targetGame)
        {
            game = targetGame;
            Elapsed = 0f;
            LastAction = string.Empty;
            cooldowns.Clear();
            saintProtectionUsed.Clear();
            explodedUnits.Clear();

            foreach (BoardUnit unit in game.Units)
            {
                cooldowns[unit] = GetActionInterval(unit);
            }
        }

        public void Tick(float deltaTime)
        {
            if (game == null || IsFinished || deltaTime <= 0f)
            {
                return;
            }

            float step = Math.Min(deltaTime, Duration - Elapsed);
            Elapsed += step;

            BoardUnit[] actingUnits = game.Units
                .Where(unit => unit.IsAlive)
                .OrderBy(unit => unit.PlacementOrder)
                .ToArray();

            foreach (BoardUnit unit in actingUnits)
            {
                if (!unit.IsAlive || !cooldowns.ContainsKey(unit))
                {
                    continue;
                }

                cooldowns[unit] -= step;
                while (cooldowns[unit] <= 0f && unit.IsAlive && !IsFinished)
                {
                    Act(unit);
                    cooldowns[unit] += GetActionInterval(unit);
                }
            }
        }

        public bool TryGetRemainingCooldown(BoardUnit unit, out float remainingSeconds)
        {
            if (unit != null && unit.IsAlive && cooldowns.TryGetValue(unit, out float cooldown))
            {
                remainingSeconds = Mathf.Max(0f, cooldown);
                return true;
            }

            remainingSeconds = 0f;
            return false;
        }

        public float GetActionInterval(BoardUnit unit)
        {
            if (unit == null)
            {
                return 0f;
            }

            float speedBonus = 0f;
            if (!unit.Definition.IsSupport && game != null)
            {
                foreach (BoardUnit ally in game.Units)
                {
                    if (ally != unit
                        && ally.IsAlive
                        && ally.Side == unit.Side
                        && ally.Definition.Ability == UnitAbility.HasteAura
                        && ally.DistanceTo(unit) <= ally.Definition.Range)
                    {
                        speedBonus = Mathf.Max(speedBonus, ally.Definition.AbilityRatio);
                    }
                }
            }

            return Mathf.Max(0.1f, unit.Definition.ActionInterval / (1f + speedBonus));
        }

        private void Act(BoardUnit actor)
        {
            int power = GetModifiedPower(actor);
            CombatActionPlan plan = CombatActionRules.BuildAbilityPlan(actor, game.Units, power);
            if (plan.Effects.Count == 0)
            {
                return;
            }

            var results = new List<CombatEffectResult>();
            var defeatedUnits = new List<BoardUnit>();
            foreach (CombatEffect effect in plan.Effects)
            {
                if (effect.Kind == CombatEffectKind.Damage)
                {
                    ApplyDamage(actor, effect.Target, effect.Amount, results, defeatedUnits, true);
                }
                else
                {
                    ApplyHeal(actor, effect.Target, effect.Amount, results);
                }
            }

            ReportAction(actor, plan.Kind, results);
            ProcessDefeated(defeatedUnits);
        }

        private int GetModifiedPower(BoardUnit actor)
        {
            int power = actor.Definition.Power;
            if (actor.Definition.Ability == UnitAbility.IsolatedAssault)
            {
                bool hasAdjacentAlly = game.Units.Any(unit => unit != actor
                    && unit.IsAlive
                    && unit.Side == actor.Side
                    && actor.DistanceTo(unit) <= 1);
                power = hasAdjacentAlly ? actor.Definition.Power : actor.Definition.AbilityPower;
            }

            float weakenRatio = 0f;
            foreach (BoardUnit enemy in game.Units)
            {
                if (enemy.IsAlive
                    && enemy.Side != actor.Side
                    && enemy.Definition.Ability == UnitAbility.WeakenAura
                    && enemy.DistanceTo(actor) <= enemy.Definition.Range)
                {
                    weakenRatio = Mathf.Max(weakenRatio, enemy.Definition.AbilityRatio);
                }
            }

            return Mathf.Max(0, Mathf.RoundToInt(power * (1f - weakenRatio)));
        }

        private void ApplyDamage(
            BoardUnit source,
            BoardUnit target,
            int rawAmount,
            List<CombatEffectResult> results,
            List<BoardUnit> defeatedUnits,
            bool allowRedirect)
        {
            if (target == null || !target.IsAlive || rawAmount <= 0)
            {
                return;
            }

            int amount = rawAmount;
            if (target.Definition.Ability == UnitAbility.DamageReduction)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - target.Definition.AbilityRatio)));
            }

            BoardUnit redirector = allowRedirect ? FindRedirector(target) : null;
            if (redirector != null)
            {
                int redirected = Mathf.Clamp(
                    Mathf.RoundToInt(amount * redirector.Definition.AbilityRatio),
                    1,
                    amount);
                amount -= redirected;
                if (amount > 0)
                {
                    ApplyDirectDamage(source, target, amount, results, defeatedUnits);
                }

                ApplyDamage(source, redirector, redirected, results, defeatedUnits, false);
                return;
            }

            ApplyDirectDamage(source, target, amount, results, defeatedUnits);
        }

        private void ApplyDirectDamage(
            BoardUnit source,
            BoardUnit target,
            int amount,
            List<CombatEffectResult> results,
            List<BoardUnit> defeatedUnits)
        {
            int appliedAmount = Math.Min(amount, target.CurrentHealth);
            target.TakeDamage(appliedAmount);
            UnitDamaged?.Invoke(source, target, appliedAmount);

            bool lethal = !target.IsAlive;
            if (lethal
                && target.Definition.Ability == UnitAbility.PhoenixRebirth
                && target.TryConsumeLifetimeAbility())
            {
                int restored = Mathf.Max(
                    1,
                    Mathf.RoundToInt(target.Definition.MaxHealth * target.Definition.AbilityRatio));
                target.Heal(restored);
                UnitHealed?.Invoke(target, target, restored);
                results.Add(new CombatEffectResult(target, CombatEffectKind.Damage, appliedAmount, false));
                results.Add(new CombatEffectResult(target, CombatEffectKind.Heal, restored, false));
                DamageNearbyEnemies(target, target.Definition.AbilityPower, results, defeatedUnits);
                return;
            }

            if (lethal)
            {
                BoardUnit saint = FindAvailableSaint(target);
                if (saint != null)
                {
                    saintProtectionUsed.Add(saint);
                    int restored = Mathf.Max(1, saint.Definition.AbilityPower);
                    target.Heal(restored);
                    UnitHealed?.Invoke(saint, target, restored);
                    results.Add(new CombatEffectResult(target, CombatEffectKind.Damage, appliedAmount, false));
                    results.Add(new CombatEffectResult(target, CombatEffectKind.Heal, restored, false));
                    return;
                }
            }

            results.Add(new CombatEffectResult(target, CombatEffectKind.Damage, appliedAmount, lethal));
            if (lethal && !defeatedUnits.Contains(target))
            {
                defeatedUnits.Add(target);
            }
        }

        private void ApplyHeal(
            BoardUnit source,
            BoardUnit target,
            int amount,
            List<CombatEffectResult> results)
        {
            if (target == null || !target.IsAlive || amount <= 0)
            {
                return;
            }

            int missingHealth = target.Definition.MaxHealth - target.CurrentHealth;
            int appliedAmount = Math.Min(amount, missingHealth);
            if (appliedAmount <= 0)
            {
                return;
            }

            target.Heal(appliedAmount);
            UnitHealed?.Invoke(source, target, appliedAmount);
            results.Add(new CombatEffectResult(target, CombatEffectKind.Heal, appliedAmount, false));
        }

        private BoardUnit FindRedirector(BoardUnit target)
        {
            if (target.Definition.Role == UnitRole.Guardian)
            {
                return null;
            }

            return game.Units
                .Where(unit => unit.IsAlive
                    && unit.Side == target.Side
                    && unit != target
                    && unit.Definition.Ability == UnitAbility.DamageRedirect
                    && unit.DistanceTo(target) <= unit.Definition.Range)
                .OrderBy(unit => unit.PlacementOrder)
                .FirstOrDefault();
        }

        private BoardUnit FindAvailableSaint(BoardUnit target)
        {
            return game.Units
                .Where(unit => unit.IsAlive
                    && unit != target
                    && unit.Side == target.Side
                    && unit.Definition.Ability == UnitAbility.SaintProtection
                    && unit.DistanceTo(target) <= unit.Definition.Range
                    && !saintProtectionUsed.Contains(unit))
                .OrderBy(unit => unit.PlacementOrder)
                .FirstOrDefault();
        }

        private void DamageNearbyEnemies(
            BoardUnit source,
            int amount,
            List<CombatEffectResult> results,
            List<BoardUnit> defeatedUnits)
        {
            BoardUnit[] targets = game.Units
                .Where(unit => unit.IsAlive
                    && unit.Side != source.Side
                    && source.DistanceTo(unit) <= 1)
                .ToArray();
            foreach (BoardUnit target in targets)
            {
                ApplyDamage(source, target, amount, results, defeatedUnits, true);
            }
        }

        private void ProcessDefeated(List<BoardUnit> defeatedUnits)
        {
            int index = 0;
            while (index < defeatedUnits.Count)
            {
                BoardUnit defeated = defeatedUnits[index++];
                if (defeated.IsAlive)
                {
                    continue;
                }

                cooldowns.Remove(defeated);
                game.RemoveUnit(defeated);

                if (defeated.Definition.Ability != UnitAbility.DeathExplosion
                    || !explodedUnits.Add(defeated))
                {
                    continue;
                }

                var explosionResults = new List<CombatEffectResult>();
                var chainDefeated = new List<BoardUnit>();
                BoardUnit[] targets = game.Units
                    .Where(unit => unit.IsAlive && defeated.DistanceTo(unit) <= 1)
                    .ToArray();
                foreach (BoardUnit target in targets)
                {
                    ApplyDamage(
                        defeated,
                        target,
                        defeated.Definition.AbilityPower,
                        explosionResults,
                        chainDefeated,
                        true);
                }

                ReportAction(defeated, UnitActionKind.Damage, explosionResults);
                foreach (BoardUnit chained in chainDefeated)
                {
                    if (!defeatedUnits.Contains(chained))
                    {
                        defeatedUnits.Add(chained);
                    }
                }
            }
        }

        private void ReportAction(
            BoardUnit actor,
            UnitActionKind kind,
            List<CombatEffectResult> results)
        {
            if (results.Count == 0)
            {
                return;
            }

            LastAction = kind == UnitActionKind.Heal
                ? $"{actor.Definition.DisplayName} heals {results.Count} unit(s)"
                : $"{actor.Definition.DisplayName} resolves {results.Count} effect(s)";
            ActionResolved?.Invoke(new CombatActionEvent(actor, kind, results.ToArray()));
        }
    }

    public readonly struct CombatEffectResult
    {
        public BoardUnit Target { get; }
        public CombatEffectKind Kind { get; }
        public int Amount { get; }
        public bool IsLethal { get; }

        public CombatEffectResult(
            BoardUnit target,
            CombatEffectKind kind,
            int amount,
            bool isLethal)
        {
            Target = target;
            Kind = kind;
            Amount = amount;
            IsLethal = isLethal;
        }
    }

    public sealed class CombatActionEvent
    {
        public BoardUnit Actor { get; }
        public UnitActionKind Kind { get; }
        public IReadOnlyList<CombatEffectResult> Results { get; }

        public CombatActionEvent(
            BoardUnit actor,
            UnitActionKind kind,
            IReadOnlyList<CombatEffectResult> results)
        {
            Actor = actor;
            Kind = kind;
            Results = results ?? Array.Empty<CombatEffectResult>();
        }
    }
}