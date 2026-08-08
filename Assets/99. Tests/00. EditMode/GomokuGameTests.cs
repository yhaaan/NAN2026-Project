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
            Assert.That(game.TurnNumber, Is.EqualTo(1));
        }

        [Test]
        public void TryPlace_PlacesUnitsAndStartsCombatAfterBothSides()
        {
            var game = new GomokuGame();

            Assert.That(game.TryPlace(7, 7, unit), Is.True);
            Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.Black));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(game.TurnNumber, Is.EqualTo(1));

            Assert.That(game.TryPlace(8, 7, unit), Is.True);
            Assert.That(game.GetStone(8, 7), Is.EqualTo(StoneColor.White));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Combat));
            Assert.That(game.TurnNumber, Is.EqualTo(1));

            game.CompleteCombat();
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void StartNewGame_ResetsTurnNumberAfterCompletedCombat()
        {
            var game = new GomokuGame();
            game.TryPlace(7, 7, unit);
            game.TryPlace(8, 7, unit);
            game.CompleteCombat();

            game.StartNewGame(StoneColor.White);

            Assert.That(game.TurnNumber, Is.EqualTo(1));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
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
        public void TryPlace_FiveConnectedStartsCombatAndWinsOnlyAfterSurviving(int stepX, int stepY)
        {
            var game = new GomokuGame();
            int startX = stepY < 0 ? 4 : 3;
            int startY = stepY < 0 ? 8 : 3;

            for (int move = 0; move < 5; move++)
            {
                Assert.That(game.TryPlace(startX + move * stepX, startY + move * stepY, unit), Is.True);

                if (move < 4)
                {
                    Assert.That(game.TryPlace(move * 2, 14, unit), Is.True);
                    game.CompleteCombat();
                }
            }

            Assert.That(game.FiveChallengeSide, Is.EqualTo(StoneColor.Black));
            Assert.That(game.Winner, Is.EqualTo(StoneColor.None));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Combat));
            Assert.That(game.WinningUnits, Has.Count.EqualTo(5));

            game.CompleteCombat();

            Assert.That(game.Winner, Is.EqualTo(StoneColor.Black));
            Assert.That(game.IsGameOver, Is.True);
            for (int index = 0; index < game.WinningUnits.Count; index++)
            {
                Assert.That(game.WinningUnits[index].X, Is.EqualTo(startX + index * stepX));
                Assert.That(game.WinningUnits[index].Y, Is.EqualTo(startY + index * stepY));
            }

            Assert.That(game.TryPlace(10, 10, unit), Is.False);
        }

        [Test]
        public void TryPlace_OverlineReturnsFiveWinningUnitsContainingLastMove()
        {
            var game = new GomokuGame();
            int[] blackX = { 0, 1, 2, 4, 5 };
            int[] whiteX = { 0, 2, 4, 6, 8 };

            for (int index = 0; index < blackX.Length; index++)
            {
                Assert.That(game.TryPlace(blackX[index], 7, unit), Is.True);
                Assert.That(game.TryPlace(whiteX[index], 14, unit), Is.True);
                game.CompleteCombat();
            }

            Assert.That(game.TryPlace(3, 7, unit), Is.True);

            Assert.That(game.WinningUnits, Has.Count.EqualTo(5));
            Assert.That(game.WinningUnits, Does.Contain(game.GetUnit(3, 7)));
            Assert.That(game.WinningUnits[0], Is.SameAs(game.GetUnit(0, 7)));
            Assert.That(game.WinningUnits[4], Is.SameAs(game.GetUnit(4, 7)));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Combat));

            game.CompleteCombat();
            Assert.That(game.Winner, Is.EqualTo(StoneColor.Black));

            game.StartNewGame(StoneColor.White);
            Assert.That(game.WinningUnits, Is.Empty);
        }

        [Test]
        public void CompleteCombat_WhenChallengeLineBreaks_ContinuesWithOpponentTurn()
        {
            var game = new GomokuGame();
            for (int move = 0; move < 5; move++)
            {
                game.TryPlace(3 + move, 3, unit);
                if (move < 4)
                {
                    game.TryPlace(move * 2, 14, unit);
                    game.CompleteCombat();
                }
            }

            game.RemoveUnit(game.GetUnit(5, 3));
            game.CompleteCombat();

            Assert.That(game.IsGameOver, Is.False);
            Assert.That(game.Winner, Is.EqualTo(StoneColor.None));
            Assert.That(game.FiveChallengeSide, Is.EqualTo(StoneColor.None));
            Assert.That(game.WinningUnits, Is.Empty);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Placement));
        }

        [Test]
        public void CompleteCombat_WhenOneOfMultipleChallengeLinesSurvives_EndsGame()
        {
            var game = new GomokuGame();
            Vector2Int[] blackPositions =
            {
                new Vector2Int(5, 7),
                new Vector2Int(6, 7),
                new Vector2Int(8, 7),
                new Vector2Int(9, 7),
                new Vector2Int(7, 5),
                new Vector2Int(7, 6),
                new Vector2Int(7, 8),
                new Vector2Int(7, 9)
            };

            for (int index = 0; index < blackPositions.Length; index++)
            {
                game.TryPlace(blackPositions[index].x, blackPositions[index].y, unit);
                game.TryPlace(index * 2, 14, unit);
                game.CompleteCombat();
            }

            game.TryPlace(7, 7, unit);
            game.RemoveUnit(game.GetUnit(5, 7));
            game.CompleteCombat();

            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.Winner, Is.EqualTo(StoneColor.Black));
            Assert.That(game.WinningUnits, Has.Count.EqualTo(5));
            Assert.That(game.WinningUnits, Does.Contain(game.GetUnit(7, 5)));
            Assert.That(game.WinningUnits, Does.Contain(game.GetUnit(7, 9)));
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
