using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitInfoPanelView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image roleIconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text roleNameText;
        [SerializeField] private TMP_Text roleDescriptionText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthValueText;
        [SerializeField] private Slider cooldownSlider;
        [SerializeField] private TMP_Text cooldownValueText;
        [SerializeField] private Sprite commonPanelSprite;
        [SerializeField] private Sprite rarePanelSprite;
        [SerializeField] private Sprite epicPanelSprite;
        [SerializeField] private Sprite legendaryPanelSprite;

        [Header("Role Icons")]
        [SerializeField] private Sprite guardianRoleSprite;
        [SerializeField] private Sprite vanguardRoleSprite;
        [SerializeField] private Sprite supportRoleSprite;
        [SerializeField] private Sprite marksmanRoleSprite;
        [SerializeField] private Sprite casterRoleSprite;

        [Header("Stat Colors")]
        [SerializeField] private Color attackValueColor = Color.red;
        [SerializeField] private Color rangeValueColor = Color.blue;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float showOffset = 40f;
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideOffset = 25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.14f;

        private RectTransform panelRect;
        private Vector2 shownPosition;
        private Tween positionTween;
        private Tween fadeTween;
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
            if (panelImage != null)
            {
                panelImage.sprite = GetPanelSprite(definition.Grade);
            }

            nameText.text = definition.DisplayName;
            Sprite roleSprite = GetRoleSprite(definition.Role);
            roleIconImage.sprite = roleSprite;
            roleIconImage.preserveAspect = true;
            roleIconImage.gameObject.SetActive(roleSprite != null);
            roleNameText.text = $"-{definition.RoleDisplayName}-";
            roleDescriptionText.text = definition.RoleDescription;

            string power = definition.IsSupport
                ? definition.IsHealer ? "회복력" : "지원력"
                : "공격력";
            string attackColor = ColorUtility.ToHtmlStringRGBA(attackValueColor);
            string rangeColor = ColorUtility.ToHtmlStringRGBA(rangeValueColor);
            detailsText.text =
                $" {power}  <color=#{attackColor}><b>{definition.Power}</b></color>         "
                + $"사거리  <color=#{rangeColor}><b>{definition.Range}</b></color>";

            healthSlider.minValue = 0f;
            healthSlider.maxValue = definition.MaxHealth;
            healthSlider.SetValueWithoutNotify(unit.CurrentHealth);
            healthValueText.text =
                $"<b>{unit.CurrentHealth} / {definition.MaxHealth}</b>";

            float interval = GetActionInterval(unit, combat);
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = interval;

            if (combat != null && combat.TryGetRemainingCooldown(unit, out float remainingSeconds))
            {
                float elapsedCooldown = Mathf.Clamp(interval - remainingSeconds, 0f, interval);
                cooldownSlider.SetValueWithoutNotify(elapsedCooldown);
                cooldownValueText.text =
                    $"<b>{elapsedCooldown:0.0}s / {interval:0.0}s</b>";
            }
            else
            {
                cooldownSlider.SetValueWithoutNotify(interval);
                cooldownValueText.text =
                    $"<b>{interval:0.0}s / {interval:0.0}s</b>";
            }
        }

        private Sprite GetPanelSprite(UnitGrade grade)
        {
            switch (grade)
            {
                case UnitGrade.Rare: return rarePanelSprite;
                case UnitGrade.Epic: return epicPanelSprite;
                case UnitGrade.Legendary: return legendaryPanelSprite;
                default: return commonPanelSprite;
            }
        }
        private Sprite GetRoleSprite(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Guardian: return guardianRoleSprite;
                case UnitRole.Vanguard: return vanguardRoleSprite;
                case UnitRole.Marksman: return marksmanRoleSprite;
                case UnitRole.Caster: return casterRoleSprite;
                default: return supportRoleSprite;
            }
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
