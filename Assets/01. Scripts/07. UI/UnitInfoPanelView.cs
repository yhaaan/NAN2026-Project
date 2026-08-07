using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitInfoPanelView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Image roleColorImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthValueText;
        [SerializeField] private Slider cooldownSlider;
        [SerializeField] private Text cooldownValueText;

        public bool IsVisible => panelGroup != null && panelGroup.alpha > 0.5f;

        private void Awake()
        {
            Hide();
        }

        public void Refresh(BoardUnit unit, CombatResolver combat, StoneColor playerSide)
        {
            if (unit == null || !unit.IsAlive)
            {
                Hide();
                return;
            }

            panelGroup.alpha = 1f;
            UnitDefinitionSO definition = unit.Definition;

            roleColorImage.color = definition.RoleColor;
            nameText.text = definition.DisplayName;
            string side = unit.Side == playerSide ? "아군" : "적군";
            string power = definition.IsHealer ? "회복력" : "공격력";
            string skill = definition.IsHealer ? "범위 회복" : "기본 공격";
            detailsText.text =
                $"{side} · {definition.Role}\n\n"
                + $"{definition.Description}\n\n"
                + $"{power}  {definition.Power}    사거리  {definition.Range}\n"
                + $"스킬  {skill}";

            healthSlider.minValue = 0f;
            healthSlider.maxValue = definition.MaxHealth;
            healthSlider.SetValueWithoutNotify(unit.CurrentHealth);
            healthValueText.text = $"체력  {unit.CurrentHealth}/{definition.MaxHealth}";

            float interval = Mathf.Max(0.1f, definition.ActionInterval);
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = interval;

            if (combat != null && combat.TryGetRemainingCooldown(unit, out float remainingSeconds))
            {
                float elapsedCooldown = Mathf.Clamp(interval - remainingSeconds, 0f, interval);
                cooldownSlider.SetValueWithoutNotify(elapsedCooldown);
                cooldownValueText.text = $"쿨타임  {elapsedCooldown:0.0}초/{interval:0.0}초";
            }
            else
            {
                cooldownSlider.SetValueWithoutNotify(interval);
                cooldownValueText.text = $"쿨타임  {interval:0.0}초/{interval:0.0}초";
            }
        }

        public void Hide()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
            }
        }
    }
}
