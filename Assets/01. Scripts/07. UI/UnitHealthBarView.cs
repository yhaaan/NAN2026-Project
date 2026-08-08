using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class UnitHealthBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color allyColor = new Color(0.25f, 0.85f, 0.34f);
        [SerializeField] private Color enemyColor = new Color(0.92f, 0.18f, 0.12f);

        private RectTransform rootRect;

        public BoardUnit Unit { get; private set; }
        public float HealthRatio { get; private set; }
        public RectTransform FillRect => fillRect;
        public Image FillImage => fillImage;

        private void Awake()
        {
            rootRect = transform as RectTransform;
        }

        public void Bind(BoardUnit unit, StoneColor perspectiveSide)
        {
            Unit = unit;
            fillImage.color = unit.Side == perspectiveSide ? allyColor : enemyColor;
            Refresh();
        }

        public void Refresh()
        {
            if (Unit == null)
            {
                return;
            }

            HealthRatio = Mathf.Clamp01((float)Unit.CurrentHealth / Unit.Definition.MaxHealth);
            fillRect.anchorMax = new Vector2(HealthRatio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        public void SetLayout(Vector2 anchoredPosition, Vector2 size)
        {
            if (rootRect == null)
            {
                rootRect = transform as RectTransform;
            }

            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = anchoredPosition;
            rootRect.sizeDelta = size;
        }
    }
}
