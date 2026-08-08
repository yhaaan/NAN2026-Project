using System;
using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuComTests
    {
        private UnitDefinitionSO unit;

        [SetUp]
        public void SetUp()
        {
            unit = TestUnitFactory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(unit);
        }

        [Test]
        public void ChooseMove_TakesImmediateWinningPosition()
        {
            GomokuGame game = BuildFourStoneRows(blackRowY: 7, whiteRowY: 14);
            var com = new GomokuCom(new System.Random(1));

            ComDecision decision = com.ChooseMove(game, new[] { unit }, StoneColor.Black);

            Assert.That(decision.X, Is.EqualTo(4));
            Assert.That(decision.Y, Is.EqualTo(7));
        }

        [Test]
        public void ChooseMove_BlocksOpponentsImmediateWin()
        {
            var game = new GomokuGame();
            for (int move = 0; move < 4; move++)
            {
                game.TryPlace(move * 2, 14, unit);
                game.TryPlace(move, 7, unit);
                game.CompleteCombat();
            }
            var com = new GomokuCom(new System.Random(1));

            ComDecision decision = com.ChooseMove(game, new[] { unit }, StoneColor.Black);

            Assert.That(decision.X, Is.EqualTo(4));
            Assert.That(decision.Y, Is.EqualTo(7));
        }

        [Test]
        public void ChooseMove_ExtendsStrongOwnLineTowardFive()
        {
            var game = new GomokuGame();
            for (int move = 0; move < 3; move++)
            {
                game.TryPlace(5 + move, 7, unit);
                game.TryPlace(move == 0 ? 0 : 14, move == 2 ? 0 : 14, unit);
                game.CompleteCombat();
            }
            var com = new GomokuCom(new System.Random(1));

            ComDecision decision = com.ChooseMove(game, new[] { unit }, StoneColor.Black);

            Assert.That(decision.Y, Is.EqualTo(7));
            Assert.That(decision.X == 4 || decision.X == 8, Is.True);
        }

        [Test]
        public void ChooseMove_BlocksDevelopingOpponentLineBeforeImmediateWin()
        {
            var game = new GomokuGame();
            Vector2Int[] blackFillers =
            {
                new Vector2Int(0, 0),
                new Vector2Int(14, 14),
                new Vector2Int(0, 14)
            };

            for (int move = 0; move < 3; move++)
            {
                game.TryPlace(blackFillers[move].x, blackFillers[move].y, unit);
                game.TryPlace(5 + move, 7, unit);
                game.CompleteCombat();
            }
            var com = new GomokuCom(new System.Random(1));

            ComDecision decision = com.ChooseMove(game, new[] { unit }, StoneColor.Black);

            Assert.That(decision.Y, Is.EqualTo(7));
            Assert.That(decision.X == 4 || decision.X == 8, Is.True);
        }

        [Test]
        public void ChooseMove_PrefersDurableUnitWhenWinningStoneWillBeAttacked()
        {
            UnitDefinitionSO enemy = TestUnitFactory.Create(
                "Enemy Attacker", UnitRole.Ranged, 100, 30, 1, 1f);
            UnitDefinitionSO fragile = TestUnitFactory.Create(
                "Fragile", UnitRole.Melee, 40, 10, 1, 1f);
            UnitDefinitionSO tank = TestUnitFactory.Create(
                "Durable Tank", UnitRole.Tank, 300, 10, 1, 1f);

            try
            {
                var game = new GomokuGame();
                for (int x = 0; x < 4; x++)
                {
                    game.TryPlace(x, 7, unit);
                    game.TryPlace(x, 8, enemy);
                    game.CompleteCombat();
                }
                var com = new GomokuCom(new System.Random(1));

                ComDecision decision = com.ChooseMove(
                    game,
                    new[] { fragile, tank },
                    StoneColor.Black);

                Assert.That(decision.X, Is.EqualTo(4));
                Assert.That(decision.Y, Is.EqualTo(7));
                Assert.That(decision.OfferIndex, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(fragile);
                UnityEngine.Object.DestroyImmediate(tank);
            }
        }
        private GomokuGame BuildFourStoneRows(int blackRowY, int whiteRowY)
        {
            var game = new GomokuGame();
            for (int x = 0; x < 4; x++)
            {
                game.TryPlace(x, blackRowY, unit);
                game.TryPlace(x, whiteRowY, unit);
                game.CompleteCombat();
            }
            return game;
        }
    }
}
