using System;
using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuComTacticalTests
    {
        [Test]
        public void ChooseMove_UsesSafeSniperShotInsteadOfSuicidalThreeBlock()
        {
            UnitDefinitionSO filler = TestUnitFactory.Create(
                "Filler",
                UnitRole.Guardian,
                500,
                0,
                0,
                1f);
            UnitDefinitionSO threat = TestUnitFactory.Create(
                "Threat",
                UnitRole.Vanguard,
                30,
                100,
                1,
                0.5f);
            UnitDefinitionSO sniper = TestUnitFactory.Create(
                "Sniper",
                UnitRole.Marksman,
                60,
                40,
                5,
                2f,
                UnitGrade.Epic,
                UnitAbility.PiercingShot);

            try
            {
                GomokuGame game = BuildWhiteThreat(gameStoneCount: 3, filler, threat);
                var com = new GomokuCom(new System.Random(1));

                ComDecision decision = com.ChooseMove(
                    game,
                    new[] { sniper },
                    StoneColor.Black);

                Assert.That(
                    decision.Y == 7 && (decision.X == 4 || decision.X == 8),
                    Is.False,
                    "A fragile sniper should not body-block the ends of a three-stone line.");
                Assert.That(
                    CanSafelyShootAnyThreat(decision, 3, sniper.Range),
                    Is.True,
                    "The sniper should attack a line stone from outside adjacent enemy range.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(filler);
                UnityEngine.Object.DestroyImmediate(threat);
                UnityEngine.Object.DestroyImmediate(sniper);
            }
        }

        [Test]
        public void ChooseMove_UsesTankForForcedBlockInsteadOfExposedSniper()
        {
            UnitDefinitionSO filler = TestUnitFactory.Create(
                "Filler",
                UnitRole.Guardian,
                500,
                0,
                0,
                1f);
            UnitDefinitionSO threat = TestUnitFactory.Create(
                "Durable Threat",
                UnitRole.Vanguard,
                500,
                100,
                1,
                0.5f);
            UnitDefinitionSO sniper = TestUnitFactory.Create(
                "Exposed Sniper",
                UnitRole.Marksman,
                60,
                40,
                5,
                2f,
                UnitGrade.Epic,
                UnitAbility.PiercingShot);
            UnitDefinitionSO tank = TestUnitFactory.Create(
                "Blocking Tank",
                UnitRole.Guardian,
                500,
                8,
                1,
                1f,
                UnitGrade.Epic);

            try
            {
                GomokuGame game = BuildWhiteThreat(gameStoneCount: 4, filler, threat);
                var com = new GomokuCom(new System.Random(1));

                ComDecision decision = com.ChooseMove(
                    game,
                    new[] { sniper, tank },
                    StoneColor.Black);

                Assert.That(decision.Y, Is.EqualTo(7));
                Assert.That(decision.X == 4 || decision.X == 9, Is.True);
                Assert.That(decision.OfferIndex, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(filler);
                UnityEngine.Object.DestroyImmediate(threat);
                UnityEngine.Object.DestroyImmediate(sniper);
                UnityEngine.Object.DestroyImmediate(tank);
            }
        }

        [Test]
        public void ChooseMove_BreaksFourWithReliableSafeSniperKill()
        {
            UnitDefinitionSO filler = TestUnitFactory.Create(
                "Filler",
                UnitRole.Guardian,
                500,
                0,
                0,
                1f);
            UnitDefinitionSO threat = TestUnitFactory.Create(
                "Fragile Threat",
                UnitRole.Vanguard,
                30,
                100,
                1,
                0.5f);
            UnitDefinitionSO sniper = TestUnitFactory.Create(
                "Safe Sniper",
                UnitRole.Marksman,
                60,
                40,
                5,
                2f,
                UnitGrade.Epic,
                UnitAbility.PiercingShot);

            try
            {
                GomokuGame game = BuildWhiteThreat(gameStoneCount: 4, filler, threat);
                var com = new GomokuCom(new System.Random(1));

                ComDecision decision = com.ChooseMove(
                    game,
                    new[] { sniper },
                    StoneColor.Black);

                Assert.That(
                    decision.Y == 7 && (decision.X == 4 || decision.X == 9),
                    Is.False,
                    "A safe, lethal piercing shot should replace a suicidal direct block.");
                Assert.That(
                    CanSafelyShootAnyThreat(decision, 4, sniper.Range),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(filler);
                UnityEngine.Object.DestroyImmediate(threat);
                UnityEngine.Object.DestroyImmediate(sniper);
            }
        }

        private static GomokuGame BuildWhiteThreat(
            int gameStoneCount,
            UnitDefinitionSO filler,
            UnitDefinitionSO threat)
        {
            Vector2Int[] fillerPositions =
            {
                new Vector2Int(0, 0),
                new Vector2Int(14, 14),
                new Vector2Int(0, 14),
                new Vector2Int(14, 0)
            };
            var game = new GomokuGame();
            for (int move = 0; move < gameStoneCount; move++)
            {
                game.TryPlace(fillerPositions[move].x, fillerPositions[move].y, filler);
                game.TryPlace(5 + move, 7, threat);
                game.CompleteCombat();
            }

            return game;
        }

        private static bool CanSafelyShootAnyThreat(
            ComDecision decision,
            int threatCount,
            int range)
        {
            for (int index = 0; index < threatCount; index++)
            {
                int deltaX = Math.Abs(decision.X - (5 + index));
                int deltaY = Math.Abs(decision.Y - 7);
                int distance = Math.Max(deltaX, deltaY);
                bool rayAligned = deltaX == 0 || deltaY == 0 || deltaX == deltaY;
                if (rayAligned && distance > 1 && distance <= range)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
