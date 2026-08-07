using UnityEditor;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    internal static class TestUnitFactory
    {
        public static UnitDefinitionSO Create(
            string displayName = "Test Unit",
            UnitRole role = UnitRole.Melee,
            int maxHealth = 100,
            int power = 10,
            int range = 1,
            float actionInterval = 1f)
        {
            UnitDefinitionSO definition = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("unitId").stringValue = displayName.ToLowerInvariant().Replace(' ', '-');
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.FindProperty("power").intValue = power;
            serialized.FindProperty("range").intValue = range;
            serialized.FindProperty("actionInterval").floatValue = actionInterval;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }
    }
}
