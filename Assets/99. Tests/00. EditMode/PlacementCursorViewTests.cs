using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026.Gomoku.Tests
{
    public sealed class PlacementCursorViewTests
    {
        private const string ScenePath = "Assets/00. Scenes/GomokuMvp.unity";

        [Test]
        public void CursorAndBoardPresentationFollowPointerContext()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GomokuHud hud = FindInScene<GomokuHud>(scene);
            GomokuBoardView boardView = FindInScene<GomokuBoardView>(scene);
            PlacementCursorView cursorView = FindInScene<PlacementCursorView>(scene);
            RectTransform shopRect = FindTransform(scene, "ShopPanel") as RectTransform;
            UnitDefinitionSO definition = TestUnitFactory.Create(range: 2);
            var game = new GomokuGame();

            try
            {
                Assert.That(game.TryPlace(7, 7, definition), Is.True);
                BoardUnit enemyUnit = game.GetUnit(7, 7);
                hud.BindGame(game, StoneColor.White);
                hud.HideResult();
                hud.ShowShop(new[] { definition }, ShopState.StartingGold, 0, true);
                Canvas.ForceUpdateCanvases();

                RectTransform boardRect = boardView.rectTransform;
                float boardSize = Mathf.Min(boardRect.rect.width, boardRect.rect.height);
                float margin = boardSize * 0.045f;
                float spacing = (boardSize - margin * 2f) / (GomokuGame.BoardSize - 1);

                Vector2 enemyScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(Vector3.zero));
                cursorView.UpdatePointerPresentation(enemyScreenPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.UnitHover));
                Assert.That(boardView.PointerState.HoveredUnit, Is.SameAs(enemyUnit));
                Assert.That(cursorView.IsCursorVisible, Is.True);

                Vector2 validScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(new Vector3(spacing, 0f, 0f)));
                cursorView.UpdatePointerPresentation(validScreenPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.PlacementPreview));
                Assert.That(cursorView.IsCursorVisible, Is.False);

                Vector2 invalidScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(new Vector3(spacing * 0.5f, 0f, 0f)));
                cursorView.UpdatePointerPresentation(invalidScreenPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
                Assert.That(cursorView.IsCursorVisible, Is.True);

                Vector2 shopScreenPosition = RectTransformUtility.WorldToScreenPoint(null, shopRect.position);
                cursorView.UpdatePointerPresentation(shopScreenPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
                Assert.That(cursorView.IsCursorVisible, Is.False);

                hud.HideShop();
                Assert.That(cursorView.IsCursorVisible, Is.False);
                cursorView.UpdatePointerPresentation(validScreenPosition);
                Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
                Assert.That(cursorView.IsCursorVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
