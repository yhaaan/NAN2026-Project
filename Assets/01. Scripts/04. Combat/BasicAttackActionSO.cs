using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "BasicAttack", menuName = "NAN2026/Actions/Basic Attack")]
    public sealed class BasicAttackActionSO : UnitActionSO
    {
        public override UnitActionKind Kind => UnitActionKind.Damage;

        public override CombatActionPlan BuildPlan(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            return CombatActionRules.BuildBasicAttack(actor, units);
        }
    }
}
