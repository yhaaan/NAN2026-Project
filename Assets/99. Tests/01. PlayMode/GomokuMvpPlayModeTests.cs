using System.Collections;
using System.Collections.Generic;
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

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.PlayerSide, Is.EqualTo(StoneColor.White));
            Assert.That(hud, Is.Not.Null);
            Assert.That(boardView, Is.Not.Null);
            Assert.That(shopSlots, Has.Length.EqualTo(ShopState.SlotCount));

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
