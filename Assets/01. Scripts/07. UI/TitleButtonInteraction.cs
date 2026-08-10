using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class TitleButtonInteraction : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject indicator;
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField, Min(1f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0f)] private float hoverLift = 4f;
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float clickDuration = 0.24f;

        private RectTransform rectTransform;
        private Tween motionTween;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private bool isHovered;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            basePosition = rectTransform.anchoredPosition;
            baseScale = rectTransform.localScale;
            SetIndicatorVisible(false);
        }

        private void OnDisable()
        {
            motionTween?.Kill();
            motionTween = null;
            isHovered = false;
            SetIndicatorVisible(false);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = basePosition;
                rectTransform.localScale = baseScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract() || isHovered)
            {
                return;
            }

            isHovered = true;
            SetIndicatorVisible(true);
            SoundManager.Instance.PlaySfx(hoverSfx);
            AnimateToHoverState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHovered)
            {
                return;
            }

            isHovered = false;
            SetIndicatorVisible(false);
            AnimateToRestState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanInteract() || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            PlayClickMotion();
        }

        private bool CanInteract()
        {
            return button == null || button.IsInteractable();
        }

        private void AnimateToHoverState()
        {
            KillMotionTween();
            motionTween = DOTween.Sequence()
                .Join(rectTransform.DOScale(baseScale * hoverScale, hoverDuration).SetEase(Ease.OutBack))
                .Join(TweenAnchorPosition(basePosition + Vector2.up * hoverLift, hoverDuration).SetEase(Ease.OutQuad))
                .SetTarget(this);
        }

        private void AnimateToRestState()
        {
            KillMotionTween();
            motionTween = DOTween.Sequence()
                .Join(rectTransform.DOScale(baseScale, hoverDuration).SetEase(Ease.OutQuad))
                .Join(TweenAnchorPosition(basePosition, hoverDuration).SetEase(Ease.OutQuad))
                .SetTarget(this);
        }

        private void PlayClickMotion()
        {
            KillMotionTween();

            float pressDuration = clickDuration * 0.32f;
            float releaseDuration = clickDuration - pressDuration;
            Vector3 targetScale = isHovered ? baseScale * hoverScale : baseScale;
            Vector2 targetPosition = isHovered
                ? basePosition + Vector2.up * hoverLift
                : basePosition;

            motionTween = DOTween.Sequence()
                .Append(rectTransform.DOScale(baseScale * 0.94f, pressDuration).SetEase(Ease.InQuad))
                .Join(TweenAnchorPosition(basePosition + Vector2.down * 2f, pressDuration).SetEase(Ease.InQuad))
                .Append(rectTransform.DOScale(targetScale, releaseDuration).SetEase(Ease.OutBack))
                .Join(TweenAnchorPosition(targetPosition, releaseDuration).SetEase(Ease.OutBack))
                .SetTarget(this);
        }

        private Tween TweenAnchorPosition(Vector2 targetPosition, float duration)
        {
            return DOTween.To(
                () => rectTransform.anchoredPosition,
                value => rectTransform.anchoredPosition = value,
                targetPosition,
                duration);
        }

        private void KillMotionTween()
        {
            motionTween?.Kill();
            motionTween = null;
        }

        private void SetIndicatorVisible(bool visible)
        {
            if (indicator != null)
            {
                indicator.SetActive(visible);
            }
        }
    }
}
