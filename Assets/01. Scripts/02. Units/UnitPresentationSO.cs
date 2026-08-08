using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "UnitPresentation", menuName = "NAN2026/Unit Presentation")]
    public sealed class UnitPresentationSO : ScriptableObject
    {
        [SerializeField] private UnitView worldPrefab;
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField, Min(0.1f)] private float deathDuration = 0.45f;

        public UnitView WorldPrefab => worldPrefab;
        public Color AccentColor => accentColor;
        public float DeathDuration => deathDuration;
    }
}
