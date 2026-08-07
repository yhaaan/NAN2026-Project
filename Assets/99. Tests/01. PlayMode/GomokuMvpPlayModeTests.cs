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
            Assert.That(shopSlots, Has.Length.EqualTo(ShopState.SlotCount));
            Assert.That(infoPanel, Is.Not.Null);
            Assert.That(infoPanel.IsVisible, Is.False);
            Assert.That(turnStatusView, Is.Not.Null);

            Text turnText = turnStatusView.transform.Find("TurnText").GetComponent<Text>();
            Text phaseText = turnStatusView.transform.Find("PhaseText").GetComponent<Text>();
            Text scoreText = turnStatusView.transform.Find("ScoreText").GetComponent<Text>();
            Slider combatSlider = turnStatusView.GetComponentInChildren<Slider>(true);
            Assert.That(turnText.text, Is.EqualTo("1턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(scoreText.text, Is.EqualTo("플레이어 0 : 0 적"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);

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

            FieldInfo gameField = typeof(GomokuGameController).GetField(
                "game",
                BindingFlags.Instance | BindingFlags.NonPublic);
            GomokuGame game = gameField.GetValue(controller) as GomokuGame;
            BoardUnit enemyUnit = game.Units[0];
            Vector2Int playerPosition = FindOpenAdjacentPosition(game, enemyUnit.X, enemyUnit.Y);

            InvokePrivate(controller, "HandleShopSelection", 0);
            InvokePrivate(controller, "HandleBoardClick", playerPosition.x, playerPosition.y);

            Assert.That(phaseText.text, Is.EqualTo("전투"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.True);
            Assert.That(combatSlider.value, Is.EqualTo(0f));

            yield return null;
            Assert.That(combatSlider.value, Is.GreaterThan(0f));

            float timeout = 12f;
            while (phaseText.text == "전투" && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(timeout, Is.GreaterThan(0f), "Combat did not finish within its configured duration.");
            Assert.That(turnText.text, Is.EqualTo("2턴"));
            Assert.That(phaseText.text, Is.EqualTo("적 턴"));
            Assert.That(combatSlider.gameObject.activeSelf, Is.False);
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
