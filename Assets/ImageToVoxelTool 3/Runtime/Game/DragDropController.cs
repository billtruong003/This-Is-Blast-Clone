using System;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class DragDropController : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BlastTray blastTray;
        [SerializeField] private LayerMask gridLayerMask = ~0;
        [SerializeField] private LayerMask trayLayerMask = ~0;
        [SerializeField] private float dragHeight = 0.5f;

        private BlastObject draggedBlast;
        private BlastTrayItem selectedTrayItem;
        private GameObject dragPreview;
        private bool isDragging;
        private bool inputEnabled = true;

        public bool IsDragging => isDragging;

        public event Action<BlastObject, Vector2Int> OnBlastPlaced;
        public event Action OnDragCancelled;

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
                CancelDrag();
        }

        private void Update()
        {
            if (!inputEnabled) return;

            if (Input.GetMouseButtonDown(0))
                TryStartDrag();

            if (isDragging)
                UpdateDrag();

            if (Input.GetMouseButtonUp(0) && isDragging)
                TryReleaseDrag();
        }

        private void TryStartDrag()
        {
            var ray = GetMouseRay();

            if (!Physics.Raycast(ray, out var hit, 100f, trayLayerMask)) return;

            var trayItem = hit.collider.GetComponent<BlastTrayItem>();
            if (trayItem == null || trayItem.Count <= 0) return;

            selectedTrayItem = trayItem;
            draggedBlast = blastTray.TakeBlast(trayItem.RangeIndex);

            if (draggedBlast == null) return;

            isDragging = true;
            draggedBlast.transform.position = hit.point + Vector3.up * dragHeight;

            var col = draggedBlast.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void UpdateDrag()
        {
            if (draggedBlast == null) return;

            var ray = GetMouseRay();
            var plane = new Plane(Vector3.up, Vector3.zero);

            if (!plane.Raycast(ray, out float distance)) return;

            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 snapped = gridManager.SnapToGrid(worldPoint);

            draggedBlast.transform.position = new Vector3(snapped.x, dragHeight, snapped.z);

            var gridPos = gridManager.WorldToGrid(worldPoint);
            bool canPlace = gridManager.CanPlaceBlast(gridPos);

            UpdateDragVisual(canPlace);
        }

        private void TryReleaseDrag()
        {
            if (draggedBlast == null)
            {
                CancelDrag();
                return;
            }

            var ray = GetMouseRay();
            var plane = new Plane(Vector3.up, Vector3.zero);

            if (!plane.Raycast(ray, out float distance))
            {
                CancelDrag();
                return;
            }

            Vector3 worldPoint = ray.GetPoint(distance);
            var gridPos = gridManager.WorldToGrid(worldPoint);

            if (gridManager.PlaceBlast(draggedBlast, gridPos))
            {
                OnBlastPlaced?.Invoke(draggedBlast, gridPos);
                FinishDrag();
            }
            else
            {
                CancelDrag();
            }
        }

        private void CancelDrag()
        {
            if (draggedBlast != null)
                Destroy(draggedBlast.gameObject);

            isDragging = false;
            draggedBlast = null;
            selectedTrayItem = null;
            ClearDragVisual();
            OnDragCancelled?.Invoke();
        }

        private void FinishDrag()
        {
            isDragging = false;
            draggedBlast = null;
            selectedTrayItem = null;
            ClearDragVisual();
        }

        private void UpdateDragVisual(bool canPlace)
        {
            var renderer = draggedBlast.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var color = renderer.material.color;
            color.a = canPlace ? 0.8f : 0.3f;
            renderer.material.color = color;
        }

        private void ClearDragVisual()
        {
            if (dragPreview != null)
                Destroy(dragPreview);
        }

        private Ray GetMouseRay()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;
            return gameCamera.ScreenPointToRay(Input.mousePosition);
        }
    }
}
