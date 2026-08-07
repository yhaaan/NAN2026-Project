using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuGameTests
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
            Object.DestroyImmediate(unit);
        }

        [Test]
        public void NewGame_StartsWithBlackPlacement()
        {
            var game = new GomokuGame();

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Placement));
        }

        [Test]
        public void TryPlace_PlacesUnitsAndStartsCombatAfterBothSides()
        {
            var game = new GomokuGame();

            Assert.That(game.TryPlace(7, 7, unit), Is.True);
            Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.Black));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));

            Assert.That(game.TryPlace(8, 7, unit), Is.True);
            Assert.That(game.GetStone(8, 7), Is.EqualTo(StoneColor.White));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Combat));

            game.CompleteCombat();
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
        }

        [Test]
        public void TryPlace_RejectsOccupiedOrOutsidePositionWithoutChangingTurn()
        {
            var game = new GomokuGame();
            game.TryPlace(7, 7, unit);

            Assert.That(game.TryPlace(7, 7, unit), Is.False);
            Assert.That(game.TryPlace(-1, 0, unit), Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
        }

        [Test]
        public void CanPlace_ReturnsTrueOnlyForOpenIntersectionDuringPlacement()
        {
            var game = new GomokuGame();

            Assert.That(game.CanPlace(7, 7, unit), Is.True);
            Assert.That(game.CanPlace(-1, 0, unit), Is.False);
            Assert.That(game.CanPlace(7, 7, null), Is.False);
            game.TryPlace(7, 7, unit);
            Assert.That(game.CanPlace(7, 7, unit), Is.False);
            Assert.That(game.CanPlace(8, 7, unit), Is.True);
            game.TryPlace(8, 7, unit);
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Combat));
            Assert.That(game.CanPlace(9, 7, unit), Is.False);
        }

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(1, -1)]
        public void TryPlace_FiveConnectedBlackUnitsEndsGame(int stepX, int stepY)
        {
            var game = new GomokuGame();
            int startX = stepY < 0 ? 4 : 3;
            int startY = stepY < 0 ? 8 : 3;

            for (int move = 0; move < 5; move++)
            {
                Assert.That(game.TryPlace(startX + move * stepX, startY + move * stepY, unit), Is.True);

                if (move < 4)
                {
                    Assert.That(game.TryPlace(move, 14, unit), Is.True);
                    game.CompleteCombat();
                }
            }

            Assert.That(game.Winner, Is.EqualTo(StoneColor.Black));
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.TryPlace(10, 10, unit), Is.False);
        }

        [Test]
        public void RemoveUnit_ClearsBoardIntersection()
        {
            var game = new GomokuGame();
            game.TryPlace(7, 7, unit);
            BoardUnit placedUnit = game.GetUnit(7, 7);

            game.RemoveUnit(placedUnit);

            Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.None));
            Assert.That(game.Units, Is.Empty);
        }
    }
}
