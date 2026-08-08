using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "AreaHeal", menuName = "NAN2026/Actions/Area Heal")]
    public sealed class AreaHealActionSO : UnitActionSO
    {
        public override UnitActionKind Kind => UnitActionKind.Heal;

        public override CombatActionPlan BuildPlan(
            BoardUnit actor,
            IReadOnlyList<BoardUnit> units)
        {
            return CombatActionRules.BuildAreaHeal(actor, units);
        }
    }
}
