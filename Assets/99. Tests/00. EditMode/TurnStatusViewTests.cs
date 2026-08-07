using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku.Tests
{
    public sealed class TurnStatusViewTests
    {
        private const string PrefabPath = "Assets/02. Prefabs/02. UI/TurnStatusPanel.prefab";

        private GameObject instance;
        private TurnStatusView view;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            instance = Object.Instantiate(prefab);
            view = instance.GetComponent<TurnStatusView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(instance);
        }

        [TestCase(TurnUiPhase.Player, "플레이어 턴")]
        [TestCase(TurnUiPhase.Enemy, "적 턴")]
        [TestCase(TurnUiPhase.Combat, "전투")]
        public void SetHeader_UpdatesKoreanTurnPhaseAndScore(TurnUiPhase phase, string phaseLabel)
        {
            view.SetHeader(3, phase, 1, 2);

            Assert.That(FindText("TurnText").text, Is.EqualTo("3턴"));
            Assert.That(FindText("PhaseText").text, Is.EqualTo(phaseLabel));
            Assert.That(FindText("ScoreText").text, Is.EqualTo("플레이어 1 : 2 적"));
        }

        [Test]
        public void CombatTimer_FillsLeftToRightAndHidesOutsideCombat()
        {
            Slider slider = instance.GetComponentInChildren<Slider>(true);

            view.ShowCombatTimer(10f);
            Assert.That(slider.gameObject.activeSelf, Is.True);
            Assert.That(slider.interactable, Is.False);
            Assert.That(slider.direction, Is.EqualTo(Slider.Direction.LeftToRight));
            Assert.That(slider.minValue, Is.EqualTo(0f));
            Assert.That(slider.maxValue, Is.EqualTo(10f));
            Assert.That(slider.value, Is.EqualTo(0f));

            view.SetCombatElapsed(4.25f);
            Assert.That(slider.value, Is.EqualTo(4.25f).Within(0.001f));

            view.HideCombatTimer();
            Assert.That(slider.gameObject.activeSelf, Is.False);
            Assert.That(slider.value, Is.EqualTo(0f));
        }

        private Text FindText(string objectName)
        {
            Transform child = instance.transform.Find(objectName);
            Assert.That(child, Is.Not.Null);
            return child.GetComponent<Text>();
        }
    }
}
