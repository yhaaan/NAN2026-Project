using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "UnitCatalog", menuName = "NAN2026/Unit Catalog")]
    public sealed class UnitCatalogSO : ScriptableObject
    {
        [SerializeField] private List<UnitDefinitionSO> units = new List<UnitDefinitionSO>();

        public IReadOnlyList<UnitDefinitionSO> Units => units;
    }
}
