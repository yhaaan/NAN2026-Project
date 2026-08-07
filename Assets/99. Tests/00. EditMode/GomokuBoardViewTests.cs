using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuBoardViewTests
    {
        [Test]
        public void PointerStateHandlesUnitHoverAndPlacementPreview()
        {
            UnitDefinitionSO definition = TestUnitFactory.Create(range: 2);
            var game = new GomokuGame();

            Assert.That(game.TryPlace(7, 7, definition), Is.True);
            Assert.That(game.TryPlace(8, 7, definition), Is.True);
            BoardUnit playerUnit = game.GetUnit(7, 7);
            BoardUnit enemyUnit = game.GetUnit(8, 7);

            GameObject canvasObject = null;

            try
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var boardObject = new GameObject(
                    "Board",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(GomokuBoardView));
                RectTransform boardRect = boardObject.GetComponent<RectTransform>();
                boardRect.SetParent(canvasObject.transform, false);
                boardRect.sizeDelta = new Vector2(700f, 700f);

                GomokuBoardView boardView = boardObject.GetComponent<GomokuBoardView>();
                boardView.Bind(game, StoneColor.Black, null);
                Canvas.ForceUpdateCanvases();

                Vector2 pointerPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(Vector3.zero));
                boardView.UpdatePointerPosition(pointerPosition);
                AssertUnitHover(boardView, playerUnit);

                float boardSize = Mathf.Min(boardRect.rect.width, boardRect.rect.height);
                float margin = boardSize * 0.045f;
                float spacing = (boardSize - margin * 2f) / (GomokuGame.BoardSize - 1);
                Vector3 enemyLocalPosition = new Vector3(spacing, 0f, 0f);
                pointerPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(enemyLocalPosition));
                boardView.UpdatePointerPosition(pointerPosition);
                AssertUnitHover(boardView, enemyUnit);

                var placementGame = new GomokuGame();
                Assert.That(placementGame.TryPlace(7, 7, definition), Is.True);
                BoardUnit placementEnemy = placementGame.GetUnit(7, 7);
                boardView.Bind(placementGame, StoneColor.White, null);
                boardView.SetPlacementPreview(definition);

                pointerPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(Vector3.zero));
                boardView.UpdatePointerPosition(pointerPosition);
                AssertUnitHover(boardView, placementEnemy);

                pointerPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(enemyLocalPosition));
                boardView.UpdatePointerPosition(pointerPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.PlacementPreview));
                Assert.That(boardView.PointerState.X, Is.EqualTo(8));
                Assert.That(boardView.PointerState.Y, Is.EqualTo(7));

                Vector3 betweenIntersections = new Vector3(spacing * 0.5f, 0f, 0f);
                pointerPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(betweenIntersections));
                boardView.UpdatePointerPosition(pointerPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));

                boardView.ClearPointerPosition();
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(definition);
            }
        }

        private static void AssertUnitHover(GomokuBoardView boardView, BoardUnit expectedUnit)
        {
            Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.UnitHover));
            Assert.That(boardView.PointerState.HoveredUnit, Is.SameAs(expectedUnit));
        }
    }
}
