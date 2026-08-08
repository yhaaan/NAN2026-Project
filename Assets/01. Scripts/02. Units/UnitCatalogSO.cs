using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "UnitCatalog", menuName = "NAN2026/Unit Catalog")]
    public sealed class UnitCatalogSO : ScriptableObject
    {
        [SerializeField] private List<UnitDefinitionSO> units = new List<UnitDefinitionSO>();

        public IReadOnlyList<UnitDefinitionSO> Units => units;

        public bool TryValidate(out string error)
        {
            var unitIds = new HashSet<string>();
            for (int index = 0; index < units.Count; index++)
            {
                UnitDefinitionSO unit = units[index];
                if (unit == null)
                {
                    error = $"Unit catalog entry {index} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(unit.UnitId))
                {
                    error = $"Unit catalog entry {index} has an empty unit ID.";
                    return false;
                }

                if (!unitIds.Add(unit.UnitId))
                {
                    error = $"Unit ID '{unit.UnitId}' is duplicated.";
                    return false;
                }

                if (unit.Action == null)
                {
                    error = $"Unit '{unit.UnitId}' has no action.";
                    return false;
                }

                if (unit.Presentation == null || unit.Presentation.WorldPrefab == null)
                {
                    error = $"Unit '{unit.UnitId}' has no world presentation prefab.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
