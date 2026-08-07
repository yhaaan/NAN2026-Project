using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class BoardUnit
    {
        public UnitDefinitionSO Definition { get; }
        public StoneColor Side { get; }
        public int X { get; }
        public int Y { get; }
        public int PlacementOrder { get; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public BoardUnit(UnitDefinitionSO definition, StoneColor side, int x, int y, int placementOrder)
        {
            Definition = definition;
            Side = side;
            X = x;
            Y = y;
            PlacementOrder = placementOrder;
            CurrentHealth = definition.MaxHealth;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(Definition.MaxHealth, CurrentHealth + Mathf.Max(0, amount));
        }

        public int DistanceTo(BoardUnit other)
        {
            return Mathf.Max(Mathf.Abs(X - other.X), Mathf.Abs(Y - other.Y));
        }
    }
}
