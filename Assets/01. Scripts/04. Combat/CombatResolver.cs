using System;
using System.Collections.Generic;
using System.Linq;

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

        private void Act(BoardUnit actor)
        {
            if (actor.Definition.IsHealer)
            {
                HealAllies(actor);
                return;
            }

            BoardUnit target = SelectAttackTarget(actor);
            if (target == null)
            {
                return;
            }

            int damage = Math.Min(actor.Definition.Power, target.CurrentHealth);
            target.TakeDamage(damage);
            LastAction = $"{actor.Definition.DisplayName} attacks {target.Definition.DisplayName}";
            if (damage > 0)
            {
                UnitDamaged?.Invoke(actor, target, damage);
            }

            if (!target.IsAlive)
            {
                cooldowns.Remove(target);
                game.RemoveUnit(target);
            }
        }

        private void HealAllies(BoardUnit healer)
        {
            BoardUnit[] woundedAllies = game.Units
                .Where(unit => unit.IsAlive
                    && unit.Side == healer.Side
                    && unit.CurrentHealth < unit.Definition.MaxHealth
                    && healer.DistanceTo(unit) <= healer.Definition.Range)
                .ToArray();

            foreach (BoardUnit ally in woundedAllies)
            {
                int missingHealth = ally.Definition.MaxHealth - ally.CurrentHealth;
                int healing = Math.Min(healer.Definition.Power, missingHealth);
                ally.Heal(healing);
                if (healing > 0)
                {
                    UnitHealed?.Invoke(healer, ally, healing);
                }
            }

            if (woundedAllies.Length > 0)
            {
                LastAction = $"{healer.Definition.DisplayName} heals {woundedAllies.Length} unit(s)";
            }
        }

        private BoardUnit SelectAttackTarget(BoardUnit attacker)
        {
            IEnumerable<BoardUnit> candidates = game.Units.Where(unit =>
                unit.IsAlive
                && unit.Side != attacker.Side
                && attacker.DistanceTo(unit) <= attacker.Definition.Range);

            BoardUnit[] inRange = candidates.ToArray();
            if (inRange.Any(unit => unit.Definition.Role == UnitRole.Tank))
            {
                inRange = inRange.Where(unit => unit.Definition.Role == UnitRole.Tank).ToArray();
            }

            return inRange
                .OrderBy(attacker.DistanceTo)
                .ThenBy(unit => unit.CurrentHealth)
                .ThenBy(unit => unit.PlacementOrder)
                .FirstOrDefault();
        }
    }
}
