using UnityEngine;

namespace NAN2026.Gomoku
{
    public enum UnitRole
    {
        Tank,
        Melee,
        Healer,
        Ranged
    }

    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "NAN2026/Unit Definition")]
    public sealed class UnitDefinitionSO : ScriptableObject
    {
        [SerializeField] private string unitId = "unit";
        [SerializeField] private string displayName = "Unit";
        [SerializeField, TextArea] private string description = "유닛 설명";
        [SerializeField] private UnitRole role;
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0)] private int power = 10;
        [SerializeField, Min(0)] private int range = 1;
        [SerializeField, Min(0.1f)] private float actionInterval = 1f;
        [SerializeField] private Color roleColor = Color.white;
        [SerializeField] private UnitActionSO action;
        [SerializeField] private UnitPresentationSO presentation;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public string Description => description;
        public UnitRole Role => role;
        public int MaxHealth => maxHealth;
        public int Power => power;
        public int Range => range;
        public float ActionInterval => actionInterval;
        public Color RoleColor => presentation != null ? presentation.AccentColor : roleColor;
        public UnitActionSO Action => action;
        public UnitPresentationSO Presentation => presentation;
        public bool IsHealer => action != null
            ? action.Kind == UnitActionKind.Heal
            : role == UnitRole.Healer;
    }

}
