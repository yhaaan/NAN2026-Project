using UnityEditor;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    internal static class TestUnitFactory
    {
        public static UnitDefinitionSO Create(
            string displayName = "Test Unit",
            UnitRole role = UnitRole.Vanguard,
            int maxHealth = 100,
            int power = 10,
            int range = 1,
            float actionInterval = 1f,
            UnitGrade grade = UnitGrade.Common,
            UnitAbility ability = UnitAbility.None,
            int abilityPower = 0,
            float abilityRatio = 0f)
        {
            if (role == UnitRole.Support && ability == UnitAbility.None)
            {
                ability = UnitAbility.AreaHeal;
            }

            UnitDefinitionSO definition = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("unitId").stringValue = displayName.ToLowerInvariant().Replace(' ', '-');
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("grade").enumValueIndex = (int)grade;
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("ability").enumValueIndex = (int)ability;
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.FindProperty("power").intValue = power;
            serialized.FindProperty("range").intValue = range;
            serialized.FindProperty("actionInterval").floatValue = actionInterval;
            serialized.FindProperty("abilityPower").intValue = abilityPower;
            serialized.FindProperty("abilityRatio").floatValue = abilityRatio;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}