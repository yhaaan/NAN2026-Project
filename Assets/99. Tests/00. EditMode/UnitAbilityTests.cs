using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class UnitAbilityTests
    {
        [Test]
        public void Support_DoesNotUseBasicAttack()
        {
            UnitDefinitionSO support = TestUnitFactory.Create(
                "Support", UnitRole.Support, 100, 50, 1, 1f,
                ability: UnitAbility.WeakenAura, abilityRatio: 0.3f);
            UnitDefinitionSO enemy = TestUnitFactory.Create("Enemy", power: 0, actionInterval: 10f);
            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, support);
                game.TryPlace(8, 7, enemy);
                var combat = new CombatResolver();
                combat.Begin(game);
                combat.Tick(2f);
                Assert.That(game.GetUnit(8, 7).CurrentHealth, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(support);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void BombExplosion_DamagesAdjacentAlliesAndEnemies()
        {
            UnitDefinitionSO ally = TestUnitFactory.Create("Ally", power: 0, actionInterval: 10f);
            UnitDefinitionSO filler = TestUnitFactory.Create("Filler", power: 0, actionInterval: 10f);
            UnitDefinitionSO bomb = TestUnitFactory.Create(
                "Bomb", UnitRole.Vanguard, 50, 0, 1, 10f,
                UnitGrade.Rare, UnitAbility.DeathExplosion, 40);
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", power: 100, actionInterval: 1f);
            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 8, ally);
                game.TryPlace(14, 14, filler);
                game.CompleteCombat();
                game.TryPlace(7, 7, bomb);
                game.TryPlace(8, 7, attacker);
                var combat = new CombatResolver();
                combat.Begin(game);
                combat.Tick(1.01f);
                Assert.That(game.GetUnit(7, 8).CurrentHealth, Is.EqualTo(60));
                Assert.That(game.GetUnit(8, 7).CurrentHealth, Is.EqualTo(60));
                Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.None));
            }
            finally
            {
                Object.DestroyImmediate(ally);
                Object.DestroyImmediate(filler);
                Object.DestroyImmediate(bomb);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void Ninja_UsesStrongPowerOnlyWhileIsolated()
        {
            UnitDefinitionSO ninja = TestUnitFactory.Create(
                "Ninja", UnitRole.Vanguard, 100, 6, 1, 1f,
                UnitGrade.Rare, UnitAbility.IsolatedAssault, 26);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", power: 0, actionInterval: 10f);
            try
            {
                var isolated = new GomokuGame();
                isolated.TryPlace(7, 7, ninja);
                isolated.TryPlace(8, 7, passive);
                var firstCombat = new CombatResolver();
                firstCombat.Begin(isolated);
                firstCombat.Tick(1.01f);
                Assert.That(isolated.GetUnit(8, 7).CurrentHealth, Is.EqualTo(74));

                var grouped = new GomokuGame();
                grouped.TryPlace(6, 7, passive);
                grouped.TryPlace(14, 14, passive);
                grouped.CompleteCombat();
                grouped.TryPlace(7, 7, ninja);
                grouped.TryPlace(8, 7, passive);
                var secondCombat = new CombatResolver();
                secondCombat.Begin(grouped);
                secondCombat.Tick(1.01f);
                Assert.That(grouped.GetUnit(8, 7).CurrentHealth, Is.EqualTo(94));
            }
            finally
            {
                Object.DestroyImmediate(ninja);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void PiercingShot_DamagesEveryEnemyOnBestRay()
        {
            UnitDefinitionSO sniper = TestUnitFactory.Create(
                "Sniper", UnitRole.Marksman, 100, 30, 5, 1f,
                UnitGrade.Epic, UnitAbility.PiercingShot);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", power: 0, actionInterval: 10f);
            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, sniper);
                game.TryPlace(8, 7, passive);
                game.CompleteCombat();
                game.TryPlace(0, 0, passive);
                game.TryPlace(9, 7, passive);
                var combat = new CombatResolver();
                combat.Begin(game);
                combat.Tick(1.01f);
                Assert.That(game.GetUnit(8, 7).CurrentHealth, Is.EqualTo(70));
                Assert.That(game.GetUnit(9, 7).CurrentHealth, Is.EqualTo(70));
            }
            finally
            {
                Object.DestroyImmediate(sniper);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void ShamanAndCheerleader_ModifyPowerAndAttackIntervalWithoutStacking()
        {
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", power: 100, actionInterval: 1f);
            UnitDefinitionSO shaman = TestUnitFactory.Create(
                "Shaman", UnitRole.Support, 200, 0, 2, 10f,
                UnitGrade.Epic, UnitAbility.WeakenAura, abilityRatio: 0.3f);
            UnitDefinitionSO cheerleader = TestUnitFactory.Create(
                "Cheerleader", UnitRole.Support, 100, 0, 1, 10f,
                UnitGrade.Epic, UnitAbility.HasteAura, abilityRatio: 0.3f);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", power: 0, actionInterval: 10f);
            try
            {
                var weakened = new GomokuGame();
                weakened.TryPlace(7, 7, attacker);
                weakened.TryPlace(8, 7, shaman);
                var weakenedCombat = new CombatResolver();
                weakenedCombat.Begin(weakened);
                weakenedCombat.Tick(1.01f);
                Assert.That(weakened.GetUnit(8, 7).CurrentHealth, Is.EqualTo(130));

                var hastened = new GomokuGame();
                hastened.TryPlace(6, 7, cheerleader);
                hastened.TryPlace(14, 14, passive);
                hastened.CompleteCombat();
                hastened.TryPlace(7, 7, attacker);
                hastened.TryPlace(8, 7, passive);
                var hasteCombat = new CombatResolver();
                hasteCombat.Begin(hastened);
                Assert.That(hasteCombat.GetActionInterval(hastened.GetUnit(7, 7)), Is.EqualTo(1f / 1.3f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(shaman);
                Object.DestroyImmediate(cheerleader);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void Meteor_DamagesEnemiesAroundTarget()
        {
            UnitDefinitionSO mage = TestUnitFactory.Create(
                "Mage", UnitRole.Caster, 100, 38, 3, 1f,
                UnitGrade.Epic, UnitAbility.Meteor);
            UnitDefinitionSO passive = TestUnitFactory.Create("Passive", power: 0, actionInterval: 10f);
            try
            {
                var game = new GomokuGame();
                game.TryPlace(7, 7, mage);
                game.TryPlace(8, 7, passive);
                game.CompleteCombat();
                game.TryPlace(0, 0, passive);
                game.TryPlace(8, 8, passive);
                var combat = new CombatResolver();
                combat.Begin(game);
                combat.Tick(1.01f);
                Assert.That(game.GetUnit(8, 7).CurrentHealth, Is.EqualTo(62));
                Assert.That(game.GetUnit(8, 8).CurrentHealth, Is.EqualTo(62));
            }
            finally
            {
                Object.DestroyImmediate(mage);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void LegendaryDefenses_RedirectReviveAndProtect()
        {
            UnitDefinitionSO ancient = TestUnitFactory.Create(
                "Ancient", UnitRole.Guardian, 200, 0, 1, 10f,
                UnitGrade.Legendary, UnitAbility.DamageRedirect, abilityRatio: 0.3f);
            UnitDefinitionSO phoenix = TestUnitFactory.Create(
                "Phoenix", UnitRole.Vanguard, 50, 0, 1, 10f,
                UnitGrade.Legendary, UnitAbility.PhoenixRebirth, 0, 0.5f);
            UnitDefinitionSO saint = TestUnitFactory.Create(
                "Saint", UnitRole.Support, 100, 0, 3, 10f,
                UnitGrade.Legendary, UnitAbility.SaintProtection, 30);
            UnitDefinitionSO ally = TestUnitFactory.Create("Ally", maxHealth: 50, power: 0, actionInterval: 10f);
            UnitDefinitionSO attacker = TestUnitFactory.Create("Attacker", power: 50, actionInterval: 1f);
            UnitDefinitionSO filler = TestUnitFactory.Create("Filler", power: 0, actionInterval: 10f);
            try
            {
                var redirectGame = new GomokuGame();
                redirectGame.TryPlace(6, 7, ancient);
                redirectGame.TryPlace(14, 14, filler);
                redirectGame.CompleteCombat();
                redirectGame.TryPlace(7, 7, ally);
                redirectGame.TryPlace(8, 7, attacker);
                var redirectCombat = new CombatResolver();
                redirectCombat.Begin(redirectGame);
                redirectCombat.Tick(1.01f);
                Assert.That(redirectGame.GetUnit(7, 7).CurrentHealth, Is.EqualTo(15));
                Assert.That(redirectGame.GetUnit(6, 7).CurrentHealth, Is.EqualTo(185));

                var phoenixGame = new GomokuGame();
                phoenixGame.TryPlace(7, 7, phoenix);
                phoenixGame.TryPlace(8, 7, attacker);
                var phoenixCombat = new CombatResolver();
                phoenixCombat.Begin(phoenixGame);
                phoenixCombat.Tick(1.01f);
                Assert.That(phoenixGame.GetUnit(7, 7).CurrentHealth, Is.EqualTo(25));
                Assert.That(phoenixGame.GetUnit(7, 7).LifetimeAbilityUsed, Is.True);

                var saintGame = new GomokuGame();
                saintGame.TryPlace(6, 7, saint);
                saintGame.TryPlace(14, 14, filler);
                saintGame.CompleteCombat();
                saintGame.TryPlace(7, 7, ally);
                saintGame.TryPlace(8, 7, attacker);
                var saintCombat = new CombatResolver();
                saintCombat.Begin(saintGame);
                saintCombat.Tick(1.01f);
                Assert.That(saintGame.GetUnit(7, 7).CurrentHealth, Is.EqualTo(30));
            }
            finally
            {
                Object.DestroyImmediate(ancient);
                Object.DestroyImmediate(phoenix);
                Object.DestroyImmediate(saint);
                Object.DestroyImmediate(ally);
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(filler);
            }
        }
    }
}