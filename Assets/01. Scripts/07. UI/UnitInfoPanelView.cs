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
            roleColorImage.color = definition.GradeColor;
            nameText.text = $"{definition.DisplayName}  [{definition.GradeDisplayName} · {definition.RoleDisplayName}]";

            string side = unit.Side == playerSide ? "아군" : "적군";
            string power = definition.IsSupport
                ? definition.IsHealer ? "회복력" : "지원력"
                : "공격력";
            string action = definition.IsSupport ? "지원 행동" : definition.Action?.DisplayName ?? "고유 행동";
            detailsText.text =
                $"{side} · {definition.RoleDisplayName}\n\n"
                + $"{definition.Description}\n\n"
                + $"{power}  {definition.Power}    사거리  {definition.Range}\n"
                + $"행동  {action}";

            healthSlider.minValue = 0f;
            healthSlider.maxValue = definition.MaxHealth;
            healthSlider.SetValueWithoutNotify(unit.CurrentHealth);
            healthValueText.text = $"체력  {unit.CurrentHealth}/{definition.MaxHealth}";

            float interval = combat != null
                ? combat.GetActionInterval(unit)
                : Mathf.Max(0.1f, definition.ActionInterval);
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = interval;

            if (combat != null && combat.TryGetRemainingCooldown(unit, out float remainingSeconds))
            {
                float elapsedCooldown = Mathf.Clamp(interval - remainingSeconds, 0f, interval);
                cooldownSlider.SetValueWithoutNotify(elapsedCooldown);
                cooldownValueText.text = $"쿨다운  {elapsedCooldown:0.0}초/{interval:0.0}초";
            }
            else
            {
                cooldownSlider.SetValueWithoutNotify(interval);
                cooldownValueText.text = $"쿨다운  {interval:0.0}초/{interval:0.0}초";
            }
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
