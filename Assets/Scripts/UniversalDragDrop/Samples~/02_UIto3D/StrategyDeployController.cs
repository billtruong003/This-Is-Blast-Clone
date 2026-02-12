using UnityEngine;

namespace Universal.DragDrop.Samples
{
    /// <summary>
    /// Example: Strategy game unit deployment.
    /// Listens for UI → 3D drops and spawns units on the battlefield.
    /// </summary>
    public class StrategyDeployController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridDropZone _battlefieldGrid;
        [SerializeField] private Transform _unitContainer;

        [Header("Settings")]
        [SerializeField] private string _deployChannel = "deploy";

        private void OnEnable()
        {
            DragDropEvents.OnDropAccepted += HandleDrop;
            DragDropEvents.OnSpaceTransition += HandleTransition;
        }

        private void OnDisable()
        {
            DragDropEvents.OnDropAccepted -= HandleDrop;
            DragDropEvents.OnSpaceTransition -= HandleTransition;
        }

        private void HandleDrop(IDraggable draggable, IDropZone zone, DragContext context)
        {
            // Only handle our channel
            if (draggable.Channel != _deployChannel) return;

            // Get unit data from the draggable
            var unitData = draggable.DragData as UnitDeployData;
            if (unitData == null || unitData.UnitPrefab == null) return;

            // Get grid position
            Vector3 spawnPos = context.CurrentWorldPosition;
            if (zone is GridDropZone gridZone)
            {
                Vector2Int cell = gridZone.WorldToCell(spawnPos);
                if (!gridZone.IsCellAvailable(cell))
                {
                    Debug.Log("Cell is occupied!");
                    return;
                }

                spawnPos = gridZone.CellToWorld(cell);
                gridZone.OccupyCell(cell, draggable);
            }

            // Spawn the unit
            var unit = Instantiate(unitData.UnitPrefab, spawnPos, Quaternion.identity);
            if (_unitContainer != null)
                unit.transform.SetParent(_unitContainer);

            Debug.Log($"Deployed {unitData.UnitName} at {spawnPos}");

            // Optionally reduce available count in UI
            // unitData.RemainingCount--;
        }

        private void HandleTransition(IDraggable draggable, DragSpace from, DragSpace to, DragContext context)
        {
            if (draggable.Channel != _deployChannel) return;

            if (from == DragSpace.UI && to == DragSpace.World3D)
            {
                Debug.Log("Unit card entering battlefield!");
                // Could play a sound, show range indicator, etc.
            }
        }
    }

    /// <summary>
    /// Data class for unit deployment. Assign to Draggable.DragData.
    /// </summary>
    [System.Serializable]
    public class UnitDeployData
    {
        public string UnitName;
        public GameObject UnitPrefab;
        public int Cost;
        public int RemainingCount;
        public Sprite Icon;
    }
}
