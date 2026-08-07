using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void Attack_PrioritizesTankWithinRange()
        {
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", UnitRole.Ranged, 100, 25, 3, 1f);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", UnitRole.Healer, 100, 0, 0, 10f);
            UnitDefinitionSO tank = TestUnitFactory.Create("Tank", UnitRole.Tank, 200, 0, 1, 10f);
            UnitDefinitionSO target = TestUnitFactory.Create("Target", UnitRole.Melee, 70, 0, 1, 10f);

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
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", UnitRole.Melee, 100, 100, 1, 1f);
            UnitDefinitionSO target = TestUnitFactory.Create("Target", UnitRole.Melee, 50, 0, 1, 10f);

            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, attacker);
                game.TryPlace(8, 7, target);
                var combat = new CombatResolver();
                int reportedDamage = 0;
                BoardUnit reportedAttacker = null;
                BoardUnit reportedTarget = null;
                combat.UnitDamaged += (source, destination, damage) =>
                {
                    reportedAttacker = source;
                    reportedTarget = destination;
                    reportedDamage = damage;
                };
                combat.Begin(game);

                combat.Tick(1.01f);

                Assert.That(game.GetStone(8, 7), Is.EqualTo(StoneColor.None));
                Assert.That(reportedAttacker, Is.EqualTo(game.GetUnit(7, 7)));
                Assert.That(reportedTarget.Definition, Is.EqualTo(target));
                Assert.That(reportedDamage, Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Healer_RestoresAllWoundedAlliesInRange()
        {
            UnitDefinitionSO ally = TestUnitFactory.Create("Ally", UnitRole.Melee, 100, 0, 1, 10f);
            UnitDefinitionSO healer = TestUnitFactory.Create("Healer", UnitRole.Healer, 80, 15, 2, 2f);
            UnitDefinitionSO enemy = TestUnitFactory.Create("Enemy", UnitRole.Melee, 100, 0, 1, 10f);

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
            UnitDefinitionSO first = TestUnitFactory.Create("First", UnitRole.Melee, 100, 0, 1, 2f);
            UnitDefinitionSO second = TestUnitFactory.Create("Second", UnitRole.Melee, 100, 0, 1, 3f);

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
    }
}
