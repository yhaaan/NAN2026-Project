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
        [Header("Visuals")]
        [SerializeField] private Sprite roleIcon;
        [SerializeField] private Sprite whiteSprite;
        [SerializeField] private Sprite blackSprite;
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
        public Color GradeTextColor => UnitLabels.GradeTextColor(grade);
        public Sprite RoleIcon => roleIcon;
        public Sprite WhiteSprite => whiteSprite;
        public Sprite BlackSprite => blackSprite;
        public UnitActionSO Action => action;
        public UnitPresentationSO Presentation => presentation;
        public bool IsSupport => role == UnitRole.Support;
        public bool IsHealer => ability == UnitAbility.AreaHeal
            || ability == UnitAbility.LowestHealthHeal
            || ability == UnitAbility.SaintProtection;
        public string GradeDisplayName => UnitLabels.GradeName(grade);
        public string RoleDisplayName => UnitLabels.RoleName(role);
        public string RoleDescription => UnitLabels.RoleDescription(role);
        public string AbilityDisplayName => UnitLabels.AbilityName(ability);

        public Sprite GetSprite(StoneColor side)
        {
            return side == StoneColor.Black ? blackSprite : whiteSprite;
        }
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
                case UnitRole.Guardian: return "탱커";
                case UnitRole.Vanguard: return "전사";
                case UnitRole.Marksman: return "원거리 딜러";
                case UnitRole.Caster: return "마법";
                default: return "보조";
            }
        }

        public static string RoleDescription(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Guardian:
                    return "사거리 안의 적이 이 역할군을 우선 공격합니다.";
                case UnitRole.Vanguard:
                    return "가까운 적에게 접근해 근거리 기본 공격을 합니다.";
                case UnitRole.Marksman:
                    return "거리를 유지하며 원거리 기본 공격을 합니다.";
                case UnitRole.Caster:
                    return "기본 공격 대신 고유한 공격 주문을 사용합니다.";
                default:
                    return "기본 공격 없이 회복·강화·약화 능력으로 아군을 돕습니다.";
            }
        }

        public static string AbilityName(UnitAbility ability)
        {
            switch (ability)
            {
                case UnitAbility.AreaHeal: return "범위 회복";
                case UnitAbility.DeathExplosion: return "죽음의 폭발";
                case UnitAbility.IsolatedAssault: return "고립 강습";
                case UnitAbility.DamageReduction: return "철갑";
                case UnitAbility.LowestHealthHeal: return "위기 치유";
                case UnitAbility.PiercingShot: return "관통 사격";
                case UnitAbility.WeakenAura: return "약화의 오라";
                case UnitAbility.HasteAura: return "가속의 오라";
                case UnitAbility.Meteor: return "메테오";
                case UnitAbility.DamageRedirect: return "피해 전가";
                case UnitAbility.PhoenixRebirth: return "불사조 부활";
                case UnitAbility.ChainLightning: return "연쇄 번개";
                case UnitAbility.SaintProtection: return "성녀의 가호";
                default: return string.Empty;
            }
        }
        public static Color GradeTextColor(UnitGrade grade)
        {
            return grade == UnitGrade.Common ? Color.black : GradeColor(grade);
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
