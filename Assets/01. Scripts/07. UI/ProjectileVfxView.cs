using System;
using DG.Tweening;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    public sealed class ProjectileVfxView : MonoBehaviour
    {
        [Tooltip("이 Sprite만 교체하면 됩니다. 임포트된 PPU가 월드 크기에 그대로 적용됩니다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.01f)] private float travelDuration = 0.35f;
        [Tooltip("화면 위쪽을 가상의 높이 축으로 사용한 포물선의 최고 높이입니다.")]
        [SerializeField, Min(0f)] private float arcHeight = 0.55f;
        [Tooltip("1에 가까울수록 도착 직전 수평 속도가 줄어 더 수직으로 낙하합니다.")]
        [SerializeField, Range(0.5f, 1f)] private float descentControlBias = 0.95f;
        [SerializeField] private float angleOffset;

        private Tween travelTween;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                Debug.LogError("ProjectileVfxView requires a child SpriteRenderer.", this);
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = WorldSpriteFactory.Arrow;
            }

            spriteRenderer.sortingLayerName = "WorldVfx";
            spriteRenderer.sortingOrder = 1;
        }

        public void Play(Vector3 startLocalPosition, Vector3 endLocalPosition, Action arrived)
        {
            transform.localPosition = startLocalPosition;
            Vector3 controlPoint = Vector3.Lerp(
                    startLocalPosition,
                    endLocalPosition,
                    descentControlBias)
                + Vector3.up * (arcHeight * 2f);
            float progress = 0f;
            ApplyTrajectory(startLocalPosition, controlPoint, endLocalPosition, progress);

            travelTween = DOTween.To(
                    () => progress,
                    value =>
                    {
                        progress = value;
                        ApplyTrajectory(
                            startLocalPosition,
                            controlPoint,
                            endLocalPosition,
                            progress);
                    },
                    1f,
                    travelDuration)
                // A linear parameter gives the quadratic height term a constant
                // downward acceleration, like gravity along a screen-space Z axis.
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    transform.localPosition = endLocalPosition;
                    travelTween = null;
                    arrived?.Invoke();
                    Destroy(gameObject);
                });
        }

        private void ApplyTrajectory(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float progress)
        {
            float inverse = 1f - progress;
            transform.localPosition = inverse * inverse * start
                + 2f * inverse * progress * control
                + progress * progress * end;

            Vector3 tangent = 2f * inverse * (control - start)
                + 2f * progress * (end - control);
            if (tangent.sqrMagnitude > Mathf.Epsilon)
            {
                transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + angleOffset);
            }
        }

        private void OnDestroy()
        {
            travelTween?.Kill();
        }
    }
}
