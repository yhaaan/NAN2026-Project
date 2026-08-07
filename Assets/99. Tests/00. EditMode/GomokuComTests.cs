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
