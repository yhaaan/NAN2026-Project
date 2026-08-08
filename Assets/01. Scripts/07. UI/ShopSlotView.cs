using System;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class ShopSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image roleColor;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statsText;

        private int slotIndex;
        private Action<int> onSelected;

        public bool IsSelected { get; private set; }

        public void Initialize(int index, Action<int> selectionHandler)
        {
            slotIndex = index;
            onSelected = selectionHandler;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(slotIndex));
            statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public void Bind(UnitDefinitionSO definition, bool selected, bool interactable)
        {
            nameText.text = $"{definition.DisplayName}\n{definition.GradeDisplayName} · {definition.RoleDisplayName}";
            string action = definition.IsSupport
                ? definition.IsHealer ? "회복" : "지원"
                : "공격";
            statsText.text =
                $"{definition.Description}\n"
                + $"체력 {definition.MaxHealth}  {action} {definition.Power}\n"
                + $"사거리 {definition.Range}  주기 {definition.ActionInterval:0.0}초";
            roleColor.color = definition.GradeColor;
            button.interactable = interactable;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? new Color(1f, 0.82f, 0.35f) : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
    }
}