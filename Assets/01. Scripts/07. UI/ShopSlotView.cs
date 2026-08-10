using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class ShopSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color SelectedTint = new Color(1f, 0.9f, 0.62f);
        private static readonly Color SelectedOutline = new Color(1f, 0.72f, 0.22f, 0.95f);
        private static readonly Color HoverOutline = new Color(0.76f, 0.84f, 1f, 0.8f);

        [SerializeField] private Button button;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image roleColor;
        [SerializeField] private Image roleIcon;
        [SerializeField] private Image healthStatIcon;
        [SerializeField] private Image powerStatIcon;
        [SerializeField] private Image rangeStatIcon;
        [SerializeField] private Image intervalStatIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text abilityNameText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text healthStatText;
        [SerializeField] private TMP_Text powerStatText;
        [SerializeField] private TMP_Text rangeStatText;
        [SerializeField] private TMP_Text intervalStatText;
        [SerializeField] private Sprite commonPanelSprite;
        [SerializeField] private Sprite rarePanelSprite;
        [SerializeField] private Sprite epicPanelSprite;
        [SerializeField] private Sprite legendaryPanelSprite;
        [SerializeField] private Sprite guardianRoleSprite;
        [SerializeField] private Sprite vanguardRoleSprite;
        [SerializeField] private Sprite supportRoleSprite;
        [SerializeField] private Sprite marksmanRoleSprite;
        [SerializeField] private Sprite casterRoleSprite;
        [SerializeField] private Sprite healthStatSprite;
        [SerializeField] private Sprite powerStatSprite;
        [SerializeField] private Sprite rangeStatSprite;
        [SerializeField] private Sprite intervalStatSprite;
        [SerializeField] private AudioClip clickSfx;
        [SerializeField] private AudioClip hoverSfx;

        private int slotIndex;
        private Action<int> onSelected;
        private Outline stateOutline;
        private bool isHovered;
        private Color gradeBackgroundTint = Color.white;

        public bool IsSelected { get; private set; }
        public AudioClip ClickSfx => clickSfx;
        public AudioClip HoverSfx => hoverSfx;

        public void Initialize(int index, Action<int> selectionHandler)
        {
            slotIndex = index;
            onSelected = selectionHandler;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            ConfigurePresentation();
        }

        public void Bind(UnitDefinitionSO definition, bool selected, bool interactable)
        {
            nameText.text = $"<b>{definition.DisplayName}</b>";
            gradeText.text = definition.GradeDisplayName;
            Color mutedBlack = new Color32(0, 0, 0, 180);
            gradeText.color = mutedBlack;

            panelImage.sprite = GetPanelSprite(definition.Grade);

            roleIcon.sprite = GetRoleSprite(definition.Role);
            roleIcon.preserveAspect = true;
            roleIcon.gameObject.SetActive(roleIcon.sprite != null);

            bool hasAbility = definition.Ability != UnitAbility.None;
            abilityNameText.gameObject.SetActive(hasAbility);
            abilityText.gameObject.SetActive(hasAbility);
            abilityNameText.text = hasAbility
                ? definition.AbilityDisplayName
                : string.Empty;
            abilityText.text = hasAbility
                ? CreateShopSummary(definition.Description)
                : string.Empty;

            healthStatText.text = definition.MaxHealth.ToString();
            powerStatText.text = definition.Power.ToString();
            rangeStatText.text = definition.Range.ToString();
            intervalStatText.text = $"{definition.ActionInterval:0.0}s";
            abilityText.color = mutedBlack;
            healthStatText.color = mutedBlack;
            rangeStatText.color = mutedBlack;
            intervalStatText.color = mutedBlack;
            roleColor.color = definition.GradeColor;
            gradeBackgroundTint = Color.Lerp(Color.white, definition.GradeColor, 0.13f);
            button.interactable = interactable;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ColorBlock colors = button.colors;
            colors.normalColor = selected
                ? Color.Lerp(gradeBackgroundTint, SelectedTint, 0.58f)
                : gradeBackgroundTint;
            colors.highlightedColor = Color.Lerp(gradeBackgroundTint, Color.white, 0.32f);
            colors.pressedColor = Color.Lerp(gradeBackgroundTint, new Color(0.7f, 0.72f, 0.78f), 0.32f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(
                gradeBackgroundTint.r * 0.78f,
                gradeBackgroundTint.g * 0.78f,
                gradeBackgroundTint.b * 0.78f,
                0.72f);
            button.colors = colors;
            RefreshOutline();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isHovered)
            {
                SoundManager.Instance.PlaySfx(hoverSfx);
            }

            isHovered = true;
            RefreshOutline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            RefreshOutline();
        }

        private void HandleClick()
        {
            SoundManager.Instance.PlaySfx(clickSfx);
            onSelected?.Invoke(slotIndex);
        }

        private void ConfigurePresentation()
        {
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = new Color(0.09f, 0.11f, 0.15f);
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            gradeText.alignment = TextAlignmentOptions.Right;
            gradeText.enableWordWrapping = false;
            gradeText.overflowMode = TextOverflowModes.Ellipsis;

            statsText.alignment = TextAlignmentOptions.Left;
            statsText.color = new Color(0.18f, 0.21f, 0.27f);
            statsText.enableWordWrapping = false;
            statsText.overflowMode = TextOverflowModes.Truncate;
            roleColor.gameObject.SetActive(false);
            ConfigureAbilityText();
            ConfigureStatsGrid();

            ColorBlock colors = button.colors;
            colors.normalColor = gradeBackgroundTint;
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
            abilityNameText.color = nameText.color;
        }

        private void ConfigureStatsGrid()
        {
            statsText.gameObject.SetActive(false);
            ConfigureStatIcon(healthStatIcon, healthStatSprite);
            ConfigureStatIcon(powerStatIcon, powerStatSprite);
            ConfigureStatIcon(rangeStatIcon, rangeStatSprite);
            ConfigureStatIcon(intervalStatIcon, intervalStatSprite);
        }

        private static void ConfigureStatIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
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
