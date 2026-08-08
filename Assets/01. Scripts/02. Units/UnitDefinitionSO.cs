using UnityEngine;

namespace NAN2026.Gomoku
{
    public enum UnitGrade
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum UnitRole
    {
        Guardian = 0,
        Vanguard = 1,
        Support = 2,
        Marksman = 3,
        Caster = 4
    }

    public enum UnitAbility
    {
        None,
        AreaHeal,
        DeathExplosion,
        IsolatedAssault,
        DamageReduction,
        LowestHealthHeal,
        PiercingShot,
        WeakenAura,
        HasteAura,
        Meteor,
        DamageRedirect,
        PhoenixRebirth,
        ChainLightning,
        SaintProtection
    }

    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "NAN2026/Unit Definition")]
    public sealed class UnitDefinitionSO : ScriptableObject
    {
        [SerializeField] private string unitId = "unit";
        [SerializeField] private string displayName = "유닛";
        [SerializeField, TextArea] private string description = "유닛 설명";
        [SerializeField] private UnitGrade grade;
        [SerializeField] private UnitRole role;
        [SerializeField] private UnitAbility ability;
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0)] private int power = 10;
        [SerializeField, Min(0)] private int range = 1;
        [SerializeField, Min(0.1f)] private float actionInterval = 1f;
        [SerializeField, Min(0)] private int abilityPower;
        [SerializeField, Range(0f, 1f)] private float abilityRatio;
        [SerializeField] private Color roleColor = Color.white;
        [SerializeField] private UnitActionSO action;
        [SerializeField] private UnitPresentationSO presentation;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public string Description => description;
        public UnitGrade Grade => grade;
        public UnitRole Role => role;
        public UnitAbility Ability => ability;
        public int MaxHealth => maxHealth;
        public int Power => power;
        public int Range => range;
        public float ActionInterval => actionInterval;
        public int AbilityPower => abilityPower;
        public float AbilityRatio => abilityRatio;
        public Color RoleColor => presentation != null ? presentation.AccentColor : roleColor;
        public Color GradeColor => UnitLabels.GradeColor(grade);
        public UnitActionSO Action => action;
        public UnitPresentationSO Presentation => presentation;
        public bool IsSupport => role == UnitRole.Support;
        public bool IsHealer => ability == UnitAbility.AreaHeal
            || ability == UnitAbility.LowestHealthHeal
            || ability == UnitAbility.SaintProtection;
        public string GradeDisplayName => UnitLabels.GradeName(grade);
        public string RoleDisplayName => UnitLabels.RoleName(role);
    }

    public static class UnitLabels
    {
        public static string GradeName(UnitGrade grade)
        {
            switch (grade)
            {
                case UnitGrade.Rare: return "희귀";
                case UnitGrade.Epic: return "영웅";
                case UnitGrade.Legendary: return "전설";
                default: return "일반";
            }
        }

        public static string RoleName(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Guardian: return "수호군";
                case UnitRole.Vanguard: return "돌격군";
                case UnitRole.Marksman: return "사격군";
                case UnitRole.Caster: return "술사";
                default: return "지원군";
            }
        }

        public static Color GradeColor(UnitGrade grade)
        {
            switch (grade)
            {
                case UnitGrade.Rare: return new Color(0.25f, 0.55f, 1f);
                case UnitGrade.Epic: return new Color(0.72f, 0.35f, 1f);
                case UnitGrade.Legendary: return new Color(1f, 0.68f, 0.12f);
                default: return new Color(0.78f, 0.8f, 0.84f);
            }
        }
    }
}