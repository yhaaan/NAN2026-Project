using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class CombatResolver
    {
        private readonly Dictionary<BoardUnit, float> cooldowns = new Dictionary<BoardUnit, float>();
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

            foreach (BoardUnit unit in game.Units)
            {
                cooldowns[unit] = unit.Definition.ActionInterval;
            }
        }

        public void Tick(float deltaTime)
        {
            if (game == null || IsFinished || deltaTime <= 0f)
            {
                return;
            }

            float step = System.Math.Min(deltaTime, Duration - Elapsed);
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
                    cooldowns[unit] += unit.Definition.ActionInterval;
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

        private void Act(BoardUnit actor)
        {
            CombatActionPlan plan = actor.Definition.Action != null
                ? actor.Definition.Action.BuildPlan(actor, game.Units)
                : actor.Definition.IsHealer
                    ? CombatActionRules.BuildAreaHeal(actor, game.Units)
                    : CombatActionRules.BuildBasicAttack(actor, game.Units);

            var results = new List<CombatEffectResult>();
            var defeatedUnits = new List<BoardUnit>();

            foreach (CombatEffect effect in plan.Effects)
            {
                BoardUnit target = effect.Target;
                if (target == null || !target.IsAlive || effect.Amount <= 0)
                {
                    continue;
                }

                int appliedAmount;
                bool lethal = false;
                if (effect.Kind == CombatEffectKind.Damage)
                {
                    appliedAmount = Math.Min(effect.Amount, target.CurrentHealth);
                    target.TakeDamage(appliedAmount);
                    lethal = !target.IsAlive;
                    UnitDamaged?.Invoke(actor, target, appliedAmount);
                }
                else
                {
                    int missingHealth = target.Definition.MaxHealth - target.CurrentHealth;
                    appliedAmount = Math.Min(effect.Amount, missingHealth);
                    target.Heal(appliedAmount);
                    UnitHealed?.Invoke(actor, target, appliedAmount);
                }

                if (appliedAmount <= 0)
                {
                    continue;
                }

                results.Add(new CombatEffectResult(target, effect.Kind, appliedAmount, lethal));
                if (lethal)
                {
                    defeatedUnits.Add(target);
                }
            }

            if (results.Count == 0)
            {
                return;
            }

            LastAction = plan.Kind == UnitActionKind.Heal
                ? $"{actor.Definition.DisplayName} heals {results.Count} unit(s)"
                : $"{actor.Definition.DisplayName} attacks {results[0].Target.Definition.DisplayName}";
            ActionResolved?.Invoke(new CombatActionEvent(actor, plan.Kind, results.ToArray()));

            foreach (BoardUnit defeatedUnit in defeatedUnits)
            {
                cooldowns.Remove(defeatedUnit);
                game.RemoveUnit(defeatedUnit);
            }
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
