using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NAN2026.Gomoku.Tests
{
    public sealed class GomokuMvpPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneStartsWithControllerAndFiveShopSlots()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GomokuMvp", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            GomokuGameController controller = Object.FindFirstObjectByType<GomokuGameController>();
            GomokuHud hud = Object.FindFirstObjectByType<GomokuHud>();
            GomokuBoardView boardView = Object.FindFirstObjectByType<GomokuBoardView>();
            PlacementCursorView placementCursor = Object.FindFirstObjectByType<PlacementCursorView>();
            ShopSlotView[] shopSlots = Object.FindObjectsByType<ShopSlotView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            UnitInfoPanelView infoPanel = Object.FindFirstObjectByType<UnitInfoPanelView>(
                FindObjectsInactive.Include);
            TurnStatusView turnStatusView = Object.FindFirstObjectByType<TurnStatusView>(
                FindObjectsInactive.Include);

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.PlayerSide, Is.EqualTo(StoneColor.White));
            Assert.That(hud, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(boardView.WorldView, Is.Not.Null);
            Assert.That(placementCursor, Is.Not.Null);
            Assert.That(shopSlots, Has.Length.EqualTo(ShopState.SlotCount));
            Assert.That(infoPanel, Is.Not.Null);
            Assert.That(infoPanel.IsVisible, Is.False);
            Assert.That(turnStatusView, Is.Not.Null);

            Text turnText = turnStatusView.transform.Find("TurnText").GetComponent<Text>();
            Text phaseText = turnStatusView.transform.Find("PhaseText").GetComponent<Text>();
            Text scoreText = turnStatusView.transform.Find("ScoreText").GetComponent<Text>();
            Slider combatSlider = turnStatusView.GetComponentInChildren<Slider>(true);
            Transform speedButtonRoot = hud.transform.Find("CombatSpeedPanel");
            Button speedButton = speedButtonRoot.GetComponent<Button>();
            Text speedText = speedButtonRoot.Find("SpeedText").GetComponent<Text>();
            Assert.That(turnText.text, Is.EqualTo("1턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(scoreText.text, Is.EqualTo("플레이어 0 : 0 적"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x1"));

            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x2"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x3"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x4"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x5"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x1"));
            speedButton.onClick.Invoke();
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };
            var raycastResults = new List<RaycastResult>();
            Object.FindFirstObjectByType<GraphicRaycaster>().Raycast(pointer, raycastResults);
            Assert.That(raycastResults, Is.Not.Empty);

            boardView.ShowDamage(7, 7, 25, true);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "AttackDamagePopup(Clone)"), Is.Not.Null);

            boardView.ShowDamage(7, 7, 25, false);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "HitDamagePopup(Clone)"), Is.Not.Null);

            boardView.ShowHeal(7, 7, 15);
            yield return null;
            Assert.That(FindDirectChild(boardView.transform, "HealPopup(Clone)"), Is.Not.Null);

            yield return new WaitForSeconds(0.6f);
            Assert.That(phaseText.text, Is.EqualTo("플레이어 턴"));
            Assert.That(boardView.WorldView.ActiveUnitViewCount, Is.EqualTo(1));
            UnitHealthBarView[] initialHealthBars = Object.FindObjectsByType<UnitHealthBarView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(initialHealthBars, Has.Length.EqualTo(1));
            Assert.That(initialHealthBars[0].HealthRatio, Is.EqualTo(1f));
            Assert.That(initialHealthBars[0].FillRect.anchorMax.x, Is.EqualTo(1f));
            Assert.That(Camera.main.GetComponent("CameraController"), Is.Not.Null);

            RectTransform boardRect = boardView.rectTransform;
            Vector2 placementBoardPosition = boardRect.anchoredPosition;
            Vector2 placementBoardSize = boardRect.sizeDelta;
            RectTransform shopRect = hud.ShopRect;
            Vector2 shopShownPosition = shopRect.anchoredPosition;

            FieldInfo gameField = typeof(GomokuGameController).GetField(
                "game",
                BindingFlags.Instance | BindingFlags.NonPublic);
            GomokuGame game = gameField.GetValue(controller) as GomokuGame;
            BoardUnit enemyUnit = game.Units[0];
            Vector2Int playerPosition = FindOpenAdjacentPosition(game, enemyUnit.X, enemyUnit.Y);

            RectTransform infoPanelRect = infoPanel.transform as RectTransform;
            hud.enabled = false;
            infoPanel.Refresh(enemyUnit, null, controller.PlayerSide);
            Assert.That(infoPanel.Alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x + infoPanel.ShowOffset).Within(0.1f));

            yield return new WaitForSecondsRealtime(infoPanel.ShowDuration * 0.5f);
            Assert.That(infoPanel.Alpha, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(infoPanelRect.anchoredPosition.x, Is.GreaterThan(infoPanel.ShownPosition.x));

            yield return new WaitForSecondsRealtime(infoPanel.ShowDuration * 0.6f);
            yield return new WaitForEndOfFrame();
            Assert.That(infoPanel.Alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x).Within(0.1f));

            infoPanel.Hide();
            yield return new WaitForSecondsRealtime(infoPanel.HideDuration * 0.5f);
            Assert.That(infoPanel.Alpha, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(infoPanelRect.anchoredPosition.x, Is.GreaterThan(infoPanel.ShownPosition.x));

            yield return new WaitForSecondsRealtime(infoPanel.HideDuration * 0.6f);
            yield return new WaitForEndOfFrame();
            Assert.That(infoPanel.Alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                infoPanelRect.anchoredPosition.x,
                Is.EqualTo(infoPanel.ShownPosition.x + infoPanel.HideOffset).Within(0.1f));
            hud.enabled = true;

            InvokePrivate(controller, "HandleShopSelection", 0);
            ShopSlotView selectedSlot = null;
            foreach (ShopSlotView shopSlot in shopSlots)
            {
                if (shopSlot.IsSelected)
                {
                    selectedSlot = shopSlot;
                    break;
                }
            }

            Assert.That(selectedSlot, Is.Not.Null);
            placementCursor.UpdatePointerPresentation(
                new Vector2(Screen.width * 0.5f, Screen.height * 0.95f));
            Assert.That(placementCursor.IsCursorVisible, Is.True);
            InvokePrivate(controller, "HandleBoardClick", playerPosition.x, playerPosition.y);

            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(selectedSlot.IsSelected, Is.False);
            Assert.That(placementCursor.IsCursorVisible, Is.False);
            Assert.That(boardView.PointerState.Mode, Is.EqualTo(BoardPointerMode.None));
            Assert.That(boardView.WorldView.ActiveUnitViewCount, Is.EqualTo(2));
            UnitHealthBarView[] combatHealthBars = Object.FindObjectsByType<UnitHealthBarView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(combatHealthBars, Has.Length.EqualTo(2));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);

            yield return new WaitForSecondsRealtime(
                controller.ShopHideDelayAfterPlacement * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.anchoredPosition.x, Is.EqualTo(shopShownPosition.x).Within(0.1f));
            Assert.That(shopRect.anchoredPosition.y, Is.EqualTo(shopShownPosition.y).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));

            yield return new WaitForSecondsRealtime(
                controller.ShopHideDelayAfterPlacement * 0.5f
                + hud.ShopHideDuration * 0.7f);
            yield return new WaitForEndOfFrame();
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.anchoredPosition.y, Is.LessThan(shopShownPosition.y));
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));
            AssertUnitPresentationAlignment(boardView, combatHealthBars);

            float shopTransitionTimeout = 1f;
            while (shopRect.gameObject.activeSelf && shopTransitionTimeout > 0f)
            {
                shopTransitionTimeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(shopTransitionTimeout, Is.GreaterThan(0f));
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            yield return new WaitForSecondsRealtime(hud.ShopHideDuration * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(boardRect.sizeDelta.x, Is.GreaterThan(placementBoardSize.x));
            Assert.That(combatSlider.value, Is.EqualTo(0f));
            AssertUnitPresentationAlignment(boardView, combatHealthBars);

            while (combatSlider.value <= 0f && shopTransitionTimeout > 0f)
            {
                shopTransitionTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(shopTransitionTimeout, Is.GreaterThan(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Bounds boardBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                hud.transform,
                boardRect);
            Bounds turnBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                hud.transform,
                turnStatusView.transform);
            Assert.That(boardBounds.max.y, Is.LessThan(turnBounds.min.y));

            Assert.That(combatSlider.value, Is.GreaterThan(0f));

            float timeout = 12f;
            while (combatSlider.value < 0.999f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "Combat did not finish within its configured duration.");
            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Vector2 combatBoardSize = boardRect.sizeDelta;

            yield return new WaitForSecondsRealtime(controller.CombatEndDelay * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(combatBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(combatBoardSize.y).Within(0.1f));

            while (phaseText.text == "전투" && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "Combat did not finish within its configured duration.");
            Assert.That(turnText.text, Is.EqualTo("2턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);
            Assert.That(speedButtonRoot.gameObject.activeSelf, Is.True);
            Assert.That(speedText.text, Is.EqualTo("x2"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(shopRect.gameObject.activeSelf, Is.False);

            yield return new WaitForSecondsRealtime(hud.ShopShowDuration * 0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(shopRect.gameObject.activeSelf, Is.False);
            Assert.That(boardRect.sizeDelta.x, Is.LessThan(combatBoardSize.x));
            Assert.That(boardRect.sizeDelta.x, Is.GreaterThan(placementBoardSize.x));

            float shopShowTimeout = 1f;
            while (!shopRect.gameObject.activeSelf && shopShowTimeout > 0f)
            {
                shopShowTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(shopShowTimeout, Is.GreaterThan(0f));
            Assert.That(shopRect.gameObject.activeSelf, Is.True);
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(placementBoardPosition.x).Within(0.1f));
            Assert.That(boardRect.anchoredPosition.y, Is.EqualTo(placementBoardPosition.y).Within(0.1f));
            Assert.That(boardRect.sizeDelta.x, Is.EqualTo(placementBoardSize.x).Within(0.1f));
            Assert.That(boardRect.sizeDelta.y, Is.EqualTo(placementBoardSize.y).Within(0.1f));

            yield return new WaitForSecondsRealtime(hud.ShopShowDuration + 0.05f);
            Assert.That(shopRect.anchoredPosition.x, Is.EqualTo(shopShownPosition.x).Within(0.1f));
            Assert.That(shopRect.anchoredPosition.y, Is.EqualTo(shopShownPosition.y).Within(0.1f));

            Object.Destroy(boardView.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnitViewDotweenAnimationsCompleteAndRestoreState()
        {
            UnitView actor = UnitView.CreateRuntimePlaceholder(null);
            UnitView target = UnitView.CreateRuntimePlaceholder(null);
            actor.transform.localPosition = Vector3.zero;
            target.transform.localPosition = Vector3.right;

            actor.PlayAction(target);
            yield return new WaitForSeconds(0.24f);

            Assert.That(Vector3.Distance(actor.transform.localPosition, Vector3.zero), Is.LessThan(0.001f));

            bool deathCompleted = false;
            actor.PlayDeath(0.1f, () => deathCompleted = true);
            yield return new WaitForSeconds(0.12f);

            Assert.That(deathCompleted, Is.True);
            Assert.That(Vector3.Distance(actor.transform.localScale, Vector3.zero), Is.LessThan(0.001f));

            Object.Destroy(actor.gameObject);
            Object.Destroy(target.gameObject);
            yield return null;
        }

        private static Vector2Int FindOpenAdjacentPosition(GomokuGame game, int centerX, int centerY)
        {
            for (int x = Mathf.Max(0, centerX - 1); x <= Mathf.Min(GomokuGame.BoardSize - 1, centerX + 1); x++)
            {
                for (int y = Mathf.Max(0, centerY - 1); y <= Mathf.Min(GomokuGame.BoardSize - 1, centerY + 1); y++)
                {
                    if (game.GetUnit(x, y) == null)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Assert.Fail("No open adjacent board position was found.");
            return default;
        }

        private static void AssertUnitPresentationAlignment(
            GomokuBoardView boardView,
            IEnumerable<UnitHealthBarView> healthBars)
        {
            RectTransform boardRect = boardView.rectTransform;
            Rect rect = boardRect.rect;
            float boardSize = Mathf.Min(rect.width, rect.height);
            float margin = boardSize * 0.045f;
            var gridRect = new Rect(
                -boardSize * 0.5f + margin,
                -boardSize * 0.5f + margin,
                boardSize - margin * 2f,
                boardSize - margin * 2f);
            float spacing = gridRect.width / (GomokuGame.BoardSize - 1);

            foreach (UnitHealthBarView healthBar in healthBars)
            {
                BoardUnit unit = healthBar.Unit;
                var cellPosition = new Vector2(
                    gridRect.xMin + unit.X * spacing,
                    gridRect.yMin + unit.Y * spacing);
                Vector2 expectedUnitScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(cellPosition));
                Vector2 actualUnitScreenPosition = Camera.main.WorldToScreenPoint(
                    boardView.WorldView.CellToWorld(unit.X, unit.Y));
                Assert.That(
                    Vector2.Distance(actualUnitScreenPosition, expectedUnitScreenPosition),
                    Is.LessThan(1.5f),
                    $"World unit at ({unit.X}, {unit.Y}) drifted from its board cell.");

                Vector2 expectedHealthScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    boardRect.TransformPoint(
                        cellPosition + Vector2.down * spacing * 0.48f));
                Vector2 actualHealthScreenPosition = RectTransformUtility.WorldToScreenPoint(
                    null,
                    healthBar.transform.position);
                Assert.That(
                    Vector2.Distance(actualHealthScreenPosition, expectedHealthScreenPosition),
                    Is.LessThan(1.5f),
                    $"Health UI at ({unit.X}, {unit.Y}) drifted from its unit.");
            }
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
