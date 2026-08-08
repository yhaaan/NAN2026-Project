using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void Attack_PrioritizesTankWithinRange()
        {
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", UnitRole.Marksman, 100, 25, 3, 1f);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", UnitRole.Support, 100, 0, 0, 10f);
            UnitDefinitionSO tank = TestUnitFactory.Create("Tank", UnitRole.Guardian, 200, 0, 1, 10f);
            UnitDefinitionSO target = TestUnitFactory.Create("Target", UnitRole.Vanguard, 70, 0, 1, 10f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, attacker);
                game.TryPlace(8, 7, tank);
                game.CompleteCombat();
                game.TryPlace(6, 7, passive);
                game.TryPlace(9, 7, target);

                var combat = new CombatResolver();
                combat.Begin(game);
                combat.Tick(1.01f);

                Assert.That(game.GetUnit(8, 7).CurrentHealth, Is.EqualTo(175));
                Assert.That(game.GetUnit(9, 7).CurrentHealth, Is.EqualTo(70));
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(tank);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void LethalAttack_RemovesUnitFromBoard()
        {
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", UnitRole.Vanguard, 100, 100, 1, 1f);
            UnitDefinitionSO target = TestUnitFactory.Create("Target", UnitRole.Vanguard, 50, 0, 1, 10f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, attacker);
                game.TryPlace(8, 7, target);
                var combat = new CombatResolver();
                int reportedDamage = 0;
                BoardUnit reportedAttacker = null;
                BoardUnit reportedTarget = null;
                CombatActionEvent reportedAction = null;
                combat.UnitDamaged += (source, destination, damage) =>
                {
                    reportedAttacker = source;
                    reportedTarget = destination;
                    reportedDamage = damage;
                };
                combat.ActionResolved += actionEvent => reportedAction = actionEvent;
                combat.Begin(game);

                combat.Tick(1.01f);

                Assert.That(game.GetStone(8, 7), Is.EqualTo(StoneColor.None));
                Assert.That(reportedAttacker, Is.EqualTo(game.GetUnit(7, 7)));
                Assert.That(reportedTarget.Definition, Is.EqualTo(target));
                Assert.That(reportedDamage, Is.EqualTo(50));
                Assert.That(reportedAction, Is.Not.Null);
                Assert.That(reportedAction.Kind, Is.EqualTo(UnitActionKind.Damage));
                Assert.That(reportedAction.Results.Count, Is.EqualTo(1));
                Assert.That(reportedAction.Results[0].IsLethal, Is.True);
                Assert.That(reportedAction.Results[0].Amount, Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Healer_ReportsAllTargetsInSingleActionEvent()
        {
            UnitDefinitionSO ally = TestUnitFactory.Create("Ally", UnitRole.Vanguard, 100, 0, 1, 10f);
            UnitDefinitionSO healer = TestUnitFactory.Create("Healer", UnitRole.Support, 80, 10, 2, 1f);
            UnitDefinitionSO enemy = TestUnitFactory.Create("Enemy", UnitRole.Vanguard, 100, 0, 1, 10f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, ally);
                game.TryPlace(14, 14, enemy);
                game.CompleteCombat();
                game.TryPlace(7, 8, healer);
                game.TryPlace(13, 14, enemy);
                BoardUnit firstAlly = game.GetUnit(7, 7);
                BoardUnit secondAlly = game.GetUnit(7, 8);
                firstAlly.TakeDamage(20);
                secondAlly.TakeDamage(20);

                var combat = new CombatResolver();
                CombatActionEvent reportedAction = null;
                combat.ActionResolved += actionEvent => reportedAction = actionEvent;
                combat.Begin(game);
                combat.Tick(1.01f);

                Assert.That(reportedAction, Is.Not.Null);
                Assert.That(reportedAction.Kind, Is.EqualTo(UnitActionKind.Heal));
                Assert.That(reportedAction.Results.Count, Is.EqualTo(2));
                Assert.That(reportedAction.Results[0].Kind, Is.EqualTo(CombatEffectKind.Heal));
                Assert.That(reportedAction.Results[1].Kind, Is.EqualTo(CombatEffectKind.Heal));
            }
            finally
            {
                Object.DestroyImmediate(ally);
                Object.DestroyImmediate(healer);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void Healer_RestoresAllWoundedAlliesInRange()
        {
            UnitDefinitionSO ally = TestUnitFactory.Create("Ally", UnitRole.Vanguard, 100, 0, 1, 10f);
            UnitDefinitionSO healer = TestUnitFactory.Create("Healer", UnitRole.Support, 80, 15, 2, 2f);
            UnitDefinitionSO enemy = TestUnitFactory.Create("Enemy", UnitRole.Vanguard, 100, 0, 1, 10f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, ally);
                game.TryPlace(14, 14, enemy);
                game.CompleteCombat();
                game.TryPlace(7, 8, healer);
                game.TryPlace(13, 14, enemy);
                BoardUnit woundedAlly = game.GetUnit(7, 7);
                woundedAlly.TakeDamage(30);

                var combat = new CombatResolver();
                BoardUnit reportedHealer = null;
                BoardUnit reportedTarget = null;
                int reportedHealing = 0;
                combat.UnitHealed += (source, destination, healing) =>
                {
                    reportedHealer = source;
                    reportedTarget = destination;
                    reportedHealing = healing;
                };
                combat.Begin(game);
                combat.Tick(2.01f);

                Assert.That(game.GetUnit(7, 7).CurrentHealth, Is.EqualTo(85));
                Assert.That(reportedHealer, Is.EqualTo(game.GetUnit(7, 8)));
                Assert.That(reportedTarget, Is.EqualTo(woundedAlly));
                Assert.That(reportedHealing, Is.EqualTo(15));
            }
            finally
            {
                Object.DestroyImmediate(ally);
                Object.DestroyImmediate(healer);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void RemainingCooldown_TracksEachUnitInRealTime()
        {
            UnitDefinitionSO first = TestUnitFactory.Create("First", UnitRole.Vanguard, 100, 0, 1, 2f);
            UnitDefinitionSO second = TestUnitFactory.Create("Second", UnitRole.Vanguard, 100, 0, 1, 3f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, first);
                game.TryPlace(8, 7, second);
                BoardUnit firstUnit = game.GetUnit(7, 7);
                BoardUnit secondUnit = game.GetUnit(8, 7);
                var combat = new CombatResolver();

                Assert.That(combat.TryGetRemainingCooldown(firstUnit, out _), Is.False);

                combat.Begin(game);
                combat.Tick(0.75f);

                Assert.That(combat.TryGetRemainingCooldown(firstUnit, out float firstRemaining), Is.True);
                Assert.That(combat.TryGetRemainingCooldown(secondUnit, out float secondRemaining), Is.True);
                Assert.That(firstRemaining, Is.EqualTo(1.25f).Within(0.001f));
                Assert.That(secondRemaining, Is.EqualTo(2.25f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void NoAttackOrHealTargets_FinishesCombatAfterOneSecond()
        {
            UnitDefinitionSO first = TestUnitFactory.Create("First", UnitRole.Vanguard, 100, 10, 1, 1f);
            UnitDefinitionSO second = TestUnitFactory.Create("Second", UnitRole.Vanguard, 100, 10, 1, 1f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(0, 0, first);
                game.TryPlace(14, 14, second);
                var combat = new CombatResolver();
                combat.Begin(game);

                combat.Tick(0.99f);

                Assert.That(combat.IsFinished, Is.False);
                Assert.That(combat.Elapsed, Is.EqualTo(0.99f).Within(0.001f));

                combat.Tick(0.02f);

                Assert.That(combat.IsFinished, Is.True);
                Assert.That(combat.Elapsed, Is.EqualTo(combat.Duration));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
