using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public enum TurnUiPhase
    {
        Player,
        Enemy,
        Combat
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class TurnStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject combatTimerRoot;
        [SerializeField] private Slider combatTimerSlider;

        public void SetHeader(
            int turnNumber,
            TurnUiPhase phase,
            int playerScore,
            int enemyScore)
        {
            turnText.text = $"{Mathf.Max(1, turnNumber)}턴";
            phaseText.text = PhaseLabel(phase);
            scoreText.text = $"{Mathf.Max(0, playerScore)} : {Mathf.Max(0, enemyScore)}";
        }

        public void ShowCombatTimer(float duration)
        {
            combatTimerSlider.minValue = 0f;
            combatTimerSlider.maxValue = Mathf.Max(0.01f, duration);
            combatTimerSlider.SetValueWithoutNotify(0f);
            combatTimerRoot.SetActive(true);
        }

        public void SetCombatElapsed(float elapsedSeconds)
        {
            combatTimerSlider.SetValueWithoutNotify(
                Mathf.Clamp(elapsedSeconds, combatTimerSlider.minValue, combatTimerSlider.maxValue));
        }

        public void HideCombatTimer()
        {
            combatTimerSlider.SetValueWithoutNotify(combatTimerSlider.minValue);
            combatTimerRoot.SetActive(false);
        }

        private static string PhaseLabel(TurnUiPhase phase)
        {
            switch (phase)
            {
                case TurnUiPhase.Player:
                    return "플레이어 턴";
                case TurnUiPhase.Enemy:
                    return "적 턴";
                default:
                    return "전투";
            }
        }
    }
}
