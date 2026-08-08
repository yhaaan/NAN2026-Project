using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class ShopSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color NormalTint = Color.white;
        private static readonly Color SelectedTint = new Color(1f, 0.9f, 0.62f);
        private static readonly Color SelectedOutline = new Color(1f, 0.72f, 0.22f, 0.95f);
        private static readonly Color HoverOutline = new Color(0.76f, 0.84f, 1f, 0.8f);

        [SerializeField] private Button button;
        [SerializeField] private Image roleColor;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statsText;

        private int slotIndex;
        private Action<int> onSelected;
        private Text abilityText;
        private Text healthStatText;
        private Text powerStatText;
        private Text rangeStatText;
        private Text intervalStatText;
        private Outline stateOutline;
        private bool isHovered;

        public bool IsSelected { get; private set; }

        public void Initialize(int index, Action<int> selectionHandler)
        {
            slotIndex = index;
            onSelected = selectionHandler;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(slotIndex));
            ConfigurePresentation();
        }

        public void Bind(UnitDefinitionSO definition, bool selected, bool interactable)
        {
            string gradeColor = ColorUtility.ToHtmlStringRGB(definition.GradeTextColor);
            nameText.text =
                $"<size=22><b>{definition.DisplayName}</b></size>\n"
                + $"<size=12><color=#{gradeColor}>■ {definition.GradeDisplayName}</color>   "
                + $"<color=#4B5568>■ {definition.RoleDisplayName}</color></size>";

            bool hasAbility = definition.Ability != UnitAbility.None;
            abilityText.gameObject.SetActive(hasAbility);
            abilityText.text = hasAbility
                ? $"<b>{definition.AbilityDisplayName}</b>\n{CreateShopSummary(definition.Description)}"
                : string.Empty;

            healthStatText.text = definition.MaxHealth.ToString();
            powerStatText.text = definition.Power.ToString();
            rangeStatText.text = definition.Range.ToString();
            intervalStatText.text = $"{definition.ActionInterval:0.0}초";
            roleColor.color = definition.GradeColor;
            button.interactable = interactable;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? SelectedTint : NormalTint;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            RefreshOutline();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            RefreshOutline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            RefreshOutline();
        }

        private void ConfigurePresentation()
        {
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.color = new Color(0.09f, 0.11f, 0.15f);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;

            statsText.alignment = TextAnchor.UpperLeft;
            statsText.color = new Color(0.18f, 0.21f, 0.27f);
            statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsText.verticalOverflow = VerticalWrapMode.Truncate;
            ConfigureAbilityText();
            ConfigureStatsGrid();

            ColorBlock colors = button.colors;
            colors.normalColor = NormalTint;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.9f);
            colors.selectedColor = SelectedTint;
            colors.disabledColor = new Color(0.7f, 0.72f, 0.78f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            stateOutline = GetComponent<Outline>();
            if (stateOutline == null)
            {
                stateOutline = gameObject.AddComponent<Outline>();
            }

            stateOutline.effectDistance = new Vector2(2f, -2f);
            stateOutline.useGraphicAlpha = true;
            RefreshOutline();
        }

        private void ConfigureAbilityText()
        {
            Transform existing = transform.Find("Ability");
            if (existing != null)
            {
                abilityText = existing.GetComponent<Text>();
            }
            else
            {
                var abilityObject = new GameObject(
                    "Ability",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                abilityObject.transform.SetParent(transform, false);
                abilityText = abilityObject.GetComponent<Text>();
            }

            RectTransform abilityRect = abilityText.rectTransform;
            abilityRect.anchorMin = new Vector2(0f, 0.32f);
            abilityRect.anchorMax = new Vector2(1f, 0.64f);
            abilityRect.anchoredPosition = new Vector2(4f, 0f);
            abilityRect.sizeDelta = new Vector2(-36f, 0f);

            abilityText.font = statsText.font;
            abilityText.fontSize = statsText.fontSize;
            abilityText.fontStyle = FontStyle.Normal;
            abilityText.color = statsText.color;
            abilityText.alignment = TextAnchor.UpperLeft;
            abilityText.horizontalOverflow = HorizontalWrapMode.Wrap;
            abilityText.verticalOverflow = VerticalWrapMode.Truncate;
            abilityText.lineSpacing = 0.92f;
            abilityText.supportRichText = true;
            abilityText.raycastTarget = false;
        }

        private void ConfigureStatsGrid()
        {
            statsText.gameObject.SetActive(false);

            CreateStatText("HealthStatIcon", 22f, 22f, 18f, 20f, "♥", new Color(0.72f, 0.2f, 0.28f), TextAnchor.MiddleCenter);
            healthStatText = CreateStatText("HealthStatValue", 42f, 22f, 54f, 20f, string.Empty, statsText.color, TextAnchor.MiddleLeft);

            CreateStatText("PowerStatIcon", 108f, 22f, 18f, 20f, "⚔", new Color(0.59f, 0.4f, 0f), TextAnchor.MiddleCenter);
            powerStatText = CreateStatText("PowerStatValue", 128f, 22f, 54f, 20f, string.Empty, statsText.color, TextAnchor.MiddleLeft);

            CreateStatText("RangeStatIcon", 22f, 2f, 18f, 20f, "◎", new Color(0.16f, 0.4f, 0.66f), TextAnchor.MiddleCenter);
            rangeStatText = CreateStatText("RangeStatValue", 42f, 2f, 54f, 20f, string.Empty, statsText.color, TextAnchor.MiddleLeft);

            // The stopwatch glyph sits low in the built-in font, so lift only the glyph by 2px.
            CreateStatText("IntervalStatIcon", 108f, 4f, 18f, 20f, "⏱", new Color(0.15f, 0.44f, 0.36f), TextAnchor.MiddleCenter);
            intervalStatText = CreateStatText("IntervalStatValue", 128f, 2f, 54f, 20f, string.Empty, statsText.color, TextAnchor.MiddleLeft);
        }

        private Text CreateStatText(
            string objectName,
            float x,
            float y,
            float width,
            float height,
            string value,
            Color color,
            TextAnchor alignment)
        {
            Transform existing = transform.Find(objectName);
            Text text;
            if (existing != null)
            {
                text = existing.GetComponent<Text>();
            }
            else
            {
                var textObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(transform, false);
                text = textObject.GetComponent<Text>();
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            text.font = statsText.font;
            text.fontSize = statsText.fontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private void RefreshOutline()
        {
            if (stateOutline == null)
            {
                return;
            }

            stateOutline.enabled = IsSelected || isHovered;
            stateOutline.effectColor = IsSelected ? SelectedOutline : HoverOutline;
        }

        private static string CreateShopSummary(string description)
        {
            const int maxCharacters = 46;
            string summary = string.IsNullOrWhiteSpace(description)
                ? string.Empty
                : description.Replace('\n', ' ').Replace('\r', ' ').Trim();
            if (summary.Length <= maxCharacters)
            {
                return summary;
            }

            int breakIndex = summary.LastIndexOf(' ', maxCharacters);
            if (breakIndex < maxCharacters / 2)
            {
                breakIndex = maxCharacters;
            }

            return summary.Substring(0, breakIndex).TrimEnd() + "…";
        }
    }
}
