using DG.Tweening;
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

        [Header("Transition")]
        [SerializeField, Min(0f)] private float showOffset = 40f;
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideOffset = 25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.14f;

        private RectTransform panelRect;
        private Vector2 shownPosition;
        private Tween positionTween;
        private Tween fadeTween;
        private Text healthIconText;
        private Text cooldownIconText;
        private bool targetVisible;

        public bool IsVisible => panelGroup != null && panelGroup.alpha > 0.5f;
        public float Alpha => panelGroup != null ? panelGroup.alpha : 0f;
        public Vector2 ShownPosition => shownPosition;
        public float ShowOffset => showOffset;
        public float ShowDuration => showDuration;
        public float HideOffset => hideOffset;
        public float HideDuration => hideDuration;

        private void Awake()
        {
            panelRect = transform as RectTransform;
            shownPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
            ConfigureSliderLabels();
            SetHiddenImmediately();
        }

        private void OnDestroy()
        {
            KillTransitions();
        }

        public void Refresh(BoardUnit unit, CombatResolver combat, StoneColor playerSide)
        {
            if (unit == null || !unit.IsAlive)
            {
                Hide();
                return;
            }

            Show();
            UnitDefinitionSO definition = unit.Definition;
            bool isAlly = unit.Side == playerSide;
            roleColorImage.color = isAlly
                ? new Color(0.25f, 0.62f, 1f)
                : new Color(1f, 0.32f, 0.36f);

            string gradeColor = ColorUtility.ToHtmlStringRGB(definition.GradeTextColor);
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.color = new Color(0.09f, 0.11f, 0.15f);
            nameText.text =
                $"<size=24><b>{definition.DisplayName}</b></size>\n"
                + $"<size=13><color=#{gradeColor}>■ {definition.GradeDisplayName}</color>   "
                + $"<color=#4B5568>■ {definition.RoleDisplayName}</color></size>";

            string power = definition.IsSupport
                ? definition.IsHealer ? "회복력" : "지원력"
                : "공격력";
            string abilityName = definition.Ability == UnitAbility.None
                ? definition.Action?.DisplayName ?? "기본 행동"
                : definition.AbilityDisplayName;
            string abilityDescription = definition.Ability == UnitAbility.None
                ? string.Empty
                : $"\n<color=#424B5A>{definition.Description}</color>";

            detailsText.alignment = TextAnchor.UpperLeft;
            detailsText.color = new Color(0.18f, 0.21f, 0.27f);
            detailsText.text =
                $"<size=17><b>{abilityName}</b></size>{abilityDescription}\n\n"
                + $"<color=#966600>⚔</color> {power}  <b>{definition.Power}</b>       "
                + $"<color=#2867A8>◎</color> 사거리  <b>{definition.Range}</b>";

            healthSlider.minValue = 0f;
            healthSlider.maxValue = definition.MaxHealth;
            healthSlider.SetValueWithoutNotify(unit.CurrentHealth);
            healthValueText.alignment = TextAnchor.MiddleLeft;
            healthValueText.color = new Color(0.12f, 0.14f, 0.18f);
            healthValueText.text =
                $"현재 HP  <b>{unit.CurrentHealth} / {definition.MaxHealth}</b>";

            float interval = GetActionInterval(unit, combat);
            cooldownValueText.alignment = TextAnchor.MiddleLeft;
            cooldownValueText.color = new Color(0.12f, 0.14f, 0.18f);
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = interval;

            if (combat != null && combat.TryGetRemainingCooldown(unit, out float remainingSeconds))
            {
                float elapsedCooldown = Mathf.Clamp(interval - remainingSeconds, 0f, interval);
                cooldownSlider.SetValueWithoutNotify(elapsedCooldown);
                cooldownValueText.text =
                    $"공격 주기  <b>{elapsedCooldown:0.0}초 / {interval:0.0}초</b>";
            }
            else
            {
                cooldownSlider.SetValueWithoutNotify(interval);
                cooldownValueText.text =
                    $"공격 주기  <b>{interval:0.0}초 / {interval:0.0}초</b>";
            }
        }

        private void ConfigureSliderLabels()
        {
            healthIconText = ConfigureSliderLabel(
                healthValueText,
                "HealthIcon",
                "♥",
                new Color(0.72f, 0.2f, 0.28f));
            cooldownIconText = ConfigureSliderLabel(
                cooldownValueText,
                "ActionIntervalIcon",
                "⏱",
                new Color(0.15f, 0.44f, 0.36f));
            cooldownIconText.rectTransform.anchoredPosition = new Vector2(12f, 2f);
        }

        private static Text ConfigureSliderLabel(
            Text valueText,
            string iconName,
            string glyph,
            Color iconColor)
        {
            RectTransform valueRect = valueText.rectTransform;
            Vector2 offsetMin = valueRect.offsetMin;
            Vector2 offsetMax = valueRect.offsetMax;
            offsetMin.x = 40f;
            offsetMax.x = -10f;
            valueRect.offsetMin = offsetMin;
            valueRect.offsetMax = offsetMax;
            valueText.alignment = TextAnchor.MiddleLeft;

            Transform existing = valueText.transform.parent.Find(iconName);
            Text iconText;
            if (existing != null)
            {
                iconText = existing.GetComponent<Text>();
            }
            else
            {
                var iconObject = new GameObject(
                    iconName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                iconObject.transform.SetParent(valueText.transform.parent, false);
                iconText = iconObject.GetComponent<Text>();
            }

            RectTransform iconRect = iconText.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(20f, 0f);

            iconText.font = valueText.font;
            iconText.fontSize = valueText.fontSize;
            iconText.fontStyle = FontStyle.Bold;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = iconColor;
            iconText.raycastTarget = false;
            iconText.text = glyph;
            iconText.transform.SetAsLastSibling();
            return iconText;
        }

        private static float GetActionInterval(BoardUnit unit, CombatResolver combat)
        {
            return combat != null
                ? combat.GetActionInterval(unit)
                : Mathf.Max(0.1f, unit.Definition.ActionInterval);
        }

        public void Hide()
        {
            if (!targetVisible || panelGroup == null)
            {
                return;
            }

            targetVisible = false;
            KillTransitions();
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            Vector2 hiddenPosition = shownPosition + Vector2.right * hideOffset;
            if (!Application.isPlaying
                || !isActiveAndEnabled
                || hideDuration <= Mathf.Epsilon)
            {
                panelRect.anchoredPosition = hiddenPosition;
                panelGroup.alpha = 0f;
                return;
            }

            positionTween = DOTween
                .To(
                    () => panelRect.anchoredPosition,
                    value => panelRect.anchoredPosition = value,
                    hiddenPosition,
                    hideDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    positionTween = null;
                    if (!targetVisible)
                    {
                        panelRect.anchoredPosition = hiddenPosition;
                    }
                });
            fadeTween = DOTween
                .To(
                    () => panelGroup.alpha,
                    value => panelGroup.alpha = value,
                    0f,
                    hideDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    fadeTween = null;
                    if (!targetVisible)
                    {
                        panelGroup.alpha = 0f;
                    }
                });
        }

        private void Show()
        {
            if (targetVisible || panelGroup == null || panelRect == null)
            {
                return;
            }

            targetVisible = true;
            KillTransitions();
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            if (panelGroup.alpha <= Mathf.Epsilon)
            {
                panelRect.anchoredPosition = shownPosition + Vector2.right * showOffset;
            }

            if (!Application.isPlaying
                || !isActiveAndEnabled
                || showDuration <= Mathf.Epsilon)
            {
                panelRect.anchoredPosition = shownPosition;
                panelGroup.alpha = 1f;
                return;
            }

            positionTween = DOTween
                .To(
                    () => panelRect.anchoredPosition,
                    value => panelRect.anchoredPosition = value,
                    shownPosition,
                    showDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    positionTween = null;
                    if (targetVisible)
                    {
                        panelRect.anchoredPosition = shownPosition;
                    }
                });
            fadeTween = DOTween
                .To(
                    () => panelGroup.alpha,
                    value => panelGroup.alpha = value,
                    1f,
                    showDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    fadeTween = null;
                    if (targetVisible)
                    {
                        panelGroup.alpha = 1f;
                    }
                });
        }

        private void SetHiddenImmediately()
        {
            targetVisible = false;
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
            }

            if (panelRect != null)
            {
                panelRect.anchoredPosition = shownPosition + Vector2.right * showOffset;
            }
        }

        private void KillTransitions()
        {
            positionTween?.Kill();
            fadeTween?.Kill();
            positionTween = null;
            fadeTween = null;
        }
    }
}
