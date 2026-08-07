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
        [SerializeField] private Text turnText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text scoreText;
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
            scoreText.text = $"플레이어 {Mathf.Max(0, playerScore)} : {Mathf.Max(0, enemyScore)} 적";
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
