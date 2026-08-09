using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuComSituationTests
    {
        [Test]
        public void ChooseMove_SelectsHealerFromFiveOffersForWoundedArmy()
        {
            UnitDefinitionSO ally = TestUnitFactory.Create(
                "Wounded Ally", UnitRole.Marksman, 100, 15, 3, 1f);
            UnitDefinitionSO passiveEnemy = TestUnitFactory.Create(
                "Passive Enemy", UnitRole.Vanguard, 100, 0, 1, 10f);
            UnitDefinitionSO[] offers =
            {
                TestUnitFactory.Create("Guardian", UnitRole.Guardian, 260, 8, 1, 1.5f),
                TestUnitFactory.Create("Vanguard", UnitRole.Vanguard, 120, 45, 1, 1f),
                TestUnitFactory.Create("Marksman", UnitRole.Marksman, 80, 35, 4, 1f),
                TestUnitFactory.Create(
                    "Caster", UnitRole.Caster, 80, 35, 3, 1f,
                    ability: UnitAbility.Meteor),
                TestUnitFactory.Create(
                    "Healer", UnitRole.Support, 80, 18, 3, 1f,
                    ability: UnitAbility.AreaHeal)
            };

            try
            {
                var game = new GomokuGame();
                for (int index = 0; index < 3; index++)
                {
                    game.TryPlace(5 + index, 7, ally);
                    game.TryPlace(12 + index, 12, passiveEnemy);
                    game.CompleteCombat();
                    game.GetUnit(5 + index, 7).TakeDamage(70);
                }

                var com = new GomokuCom(new System.Random(1));
                ComDecision decision = com.ChooseMove(game, offers, StoneColor.Black);

                Assert.That(decision.OfferIndex, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(ally);
                Object.DestroyImmediate(passiveEnemy);
                DestroyDefinitions(offers);
            }
        }

        [Test]
        public void ChooseMove_SelectsGuardianFromFiveOffersWhenBackLineIsOutnumbered()
        {
            UnitDefinitionSO backLine = TestUnitFactory.Create(
                "Back Line", UnitRole.Marksman, 80, 25, 4, 1f);
            UnitDefinitionSO enemy = TestUnitFactory.Create(
                "Heavy Enemy", UnitRole.Vanguard, 140, 60, 1, 1f);
            UnitDefinitionSO[] offers =
            {
                TestUnitFactory.Create("Vanguard A", UnitRole.Vanguard, 100, 65, 1, 1f),
                TestUnitFactory.Create("Vanguard B", UnitRole.Vanguard, 110, 60, 1, 1f),
                TestUnitFactory.Create("Marksman", UnitRole.Marksman, 75, 55, 4, 1f),
                TestUnitFactory.Create("Caster", UnitRole.Caster, 80, 55, 3, 1f),
                TestUnitFactory.Create(
                    "Guardian", UnitRole.Guardian, 320, 12, 1, 1.5f,
                    ability: UnitAbility.DamageReduction, abilityRatio: 0.4f)
            };

            try
            {
                var game = new GomokuGame();
                game.StartNewGame(StoneColor.White);
                game.TryPlace(10, 10, enemy);
                game.TryPlace(2, 2, backLine);
                game.CompleteCombat();
                game.TryPlace(11, 10, enemy);
                game.TryPlace(3, 2, backLine);
                game.CompleteCombat();
                game.TryPlace(12, 10, enemy);

                var com = new GomokuCom(new System.Random(7));
                ComDecision decision = com.ChooseMove(game, offers, StoneColor.Black);

                Assert.That(decision.OfferIndex, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(backLine);
                Object.DestroyImmediate(enemy);
                DestroyDefinitions(offers);
            }
        }

        private static void DestroyDefinitions(UnitDefinitionSO[] definitions)
        {
            foreach (UnitDefinitionSO definition in definitions)
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
