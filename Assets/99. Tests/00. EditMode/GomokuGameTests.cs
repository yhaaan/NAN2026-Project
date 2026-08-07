using NUnit.Framework;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuGameTests
    {
        [Test]
        public void NewGame_StartsWithBlack()
        {
            var game = new GomokuGame();

            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.IsGameOver, Is.False);
        }

        [Test]
        public void TryPlace_PlacesStoneAndAlternatesTurn()
        {
            var game = new GomokuGame();

            Assert.That(game.TryPlace(7, 7), Is.True);
            Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.Black));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));

            Assert.That(game.TryPlace(8, 7), Is.True);
            Assert.That(game.GetStone(8, 7), Is.EqualTo(StoneColor.White));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
        }

        [Test]
        public void TryPlace_RejectsOccupiedOrOutsidePositionWithoutChangingTurn()
        {
            var game = new GomokuGame();
            game.TryPlace(7, 7);

            Assert.That(game.TryPlace(7, 7), Is.False);
            Assert.That(game.TryPlace(-1, 0), Is.False);
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.White));
        }

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(1, -1)]
        public void TryPlace_FiveConnectedBlackStonesEndsGame(int stepX, int stepY)
        {
            var game = new GomokuGame();
            int startX = stepY < 0 ? 4 : 3;
            int startY = stepY < 0 ? 8 : 3;

            for (int move = 0; move < 5; move++)
            {
                Assert.That(game.TryPlace(startX + move * stepX, startY + move * stepY), Is.True);

                if (move < 4)
                {
                    Assert.That(game.TryPlace(move, 14), Is.True);
                }
            }

            Assert.That(game.Winner, Is.EqualTo(StoneColor.Black));
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.TryPlace(10, 10), Is.False);
        }

        [Test]
        public void Restart_ClearsBoardAndGameState()
        {
            var game = new GomokuGame();
            game.TryPlace(7, 7);

            game.Restart();

            Assert.That(game.GetStone(7, 7), Is.EqualTo(StoneColor.None));
            Assert.That(game.CurrentTurn, Is.EqualTo(StoneColor.Black));
            Assert.That(game.Winner, Is.EqualTo(StoneColor.None));
            Assert.That(game.LastMoveX, Is.EqualTo(-1));
            Assert.That(game.LastMoveY, Is.EqualTo(-1));
        }
    }
}
