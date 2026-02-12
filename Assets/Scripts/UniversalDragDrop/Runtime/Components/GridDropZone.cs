using UnityEngine;
using System.Collections.Generic;

namespace Universal.DragDrop
{
    /// <summary>
    /// A grid-based drop zone for strategy games and tower defense.
    /// Provides grid snapping, cell occupancy tracking, and visual grid preview.
    /// </summary>
    public class GridDropZone : DropZone
    {
        [Header("Grid Settings")]
        [Tooltip("Number of columns in the grid")]
        [SerializeField] private int _columns = 10;

        [Tooltip("Number of rows in the grid")]
        [SerializeField] private int _rows = 10;

        [Tooltip("Size of each cell")]
        [SerializeField] private Vector2 _cellSize = new Vector2(1f, 1f);

        [Tooltip("Origin position of the grid (bottom-left corner)")]
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;

        [Tooltip("Show grid gizmos in editor")]
        [SerializeField] private bool _showGridGizmos = true;

        [Tooltip("Color of grid gizmos")]
        [SerializeField] private Color _gridGizmoColor = new Color(1, 1, 1, 0.3f);

        [Tooltip("Color of occupied cells")]
        [SerializeField] private Color _occupiedGizmoColor = new Color(1, 0, 0, 0.3f);

        [Tooltip("Color of the highlighted cell")]
        [SerializeField] private Color _highlightGizmoColor = new Color(0, 1, 0, 0.5f);

        // ─── Runtime State ──────────────────────────────────────────
        private bool[,] _occupiedCells;
        private Dictionary<Vector2Int, IDraggable> _cellContents;
        private Vector2Int _highlightedCell = new Vector2Int(-1, -1);
        private GameObject _gridHighlight;

        public int Columns => _columns;
        public int Rows => _rows;
        public Vector2 CellSize => _cellSize;

        // ─── Initialization ─────────────────────────────────────────

        private new void Awake()
        {
            _occupiedCells = new bool[_columns, _rows];
            _cellContents = new Dictionary<Vector2Int, IDraggable>();
        }

        // ─── Grid Operations ────────────────────────────────────────

        /// <summary>Convert a world position to grid cell coordinates.</summary>
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition) - _gridOrigin;
            int col = Mathf.FloorToInt(local.x / _cellSize.x);
            int row;

            if (TargetSpace == DragSpace.World3D)
                row = Mathf.FloorToInt(local.z / _cellSize.y);
            else
                row = Mathf.FloorToInt(local.y / _cellSize.y);

            return new Vector2Int(col, row);
        }

        /// <summary>Convert grid cell coordinates to world position (center of cell).</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 local = _gridOrigin;
            local.x += (cell.x + 0.5f) * _cellSize.x;

            if (TargetSpace == DragSpace.World3D)
                local.z += (cell.y + 0.5f) * _cellSize.y;
            else
                local.y += (cell.y + 0.5f) * _cellSize.y;

            return transform.TransformPoint(local);
        }

        /// <summary>Convert grid cell to local position (center of cell).</summary>
        public Vector3 CellToLocal(Vector2Int cell)
        {
            Vector3 local = _gridOrigin;
            local.x += (cell.x + 0.5f) * _cellSize.x;

            if (TargetSpace == DragSpace.World3D)
                local.z += (cell.y + 0.5f) * _cellSize.y;
            else
                local.y += (cell.y + 0.5f) * _cellSize.y;

            return local;
        }

        /// <summary>Check if a cell is within grid bounds.</summary>
        public bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < _columns && cell.y >= 0 && cell.y < _rows;
        }

        /// <summary>Check if a cell is occupied.</summary>
        public bool IsCellOccupied(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return true;
            return _occupiedCells[cell.x, cell.y];
        }

        /// <summary>Check if a cell is free and valid.</summary>
        public bool IsCellAvailable(Vector2Int cell)
        {
            return IsValidCell(cell) && !_occupiedCells[cell.x, cell.y];
        }

        /// <summary>Occupy a cell with an item.</summary>
        public void OccupyCell(Vector2Int cell, IDraggable item)
        {
            if (!IsValidCell(cell)) return;
            _occupiedCells[cell.x, cell.y] = true;
            _cellContents[cell] = item;
        }

        /// <summary>Free a cell.</summary>
        public void FreeCell(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return;
            _occupiedCells[cell.x, cell.y] = false;
            _cellContents.Remove(cell);
        }

        /// <summary>Get the item at a specific cell.</summary>
        public IDraggable GetItemAtCell(Vector2Int cell)
        {
            _cellContents.TryGetValue(cell, out IDraggable item);
            return item;
        }

        /// <summary>Get all occupied cells.</summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < _columns; x++)
                for (int y = 0; y < _rows; y++)
                    if (_occupiedCells[x, y])
                        cells.Add(new Vector2Int(x, y));
            return cells;
        }

        /// <summary>Clear all cells.</summary>
        public void ClearGrid()
        {
            _occupiedCells = new bool[_columns, _rows];
            _cellContents.Clear();
            ClearItems();
        }

        // ─── Drop Zone Overrides ────────────────────────────────────

        /// <summary>
        /// Override snap position to snap to grid cell center.
        /// </summary>
        public new Vector3 GetSnapPosition(IDraggable draggable, DragContext context)
        {
            Vector2Int cell = WorldToCell(context.CurrentWorldPosition);

            if (!IsValidCell(cell))
            {
                // Clamp to nearest valid cell
                cell.x = Mathf.Clamp(cell.x, 0, _columns - 1);
                cell.y = Mathf.Clamp(cell.y, 0, _rows - 1);
            }

            OccupyCell(cell, draggable);
            return CellToWorld(cell);
        }

        // ─── Hover Visualization ────────────────────────────────────

        /// <summary>Highlighted cell during hover.</summary>
        public Vector2Int HighlightedCell => _highlightedCell;

        public void UpdateHighlight(Vector3 worldPosition)
        {
            Vector2Int cell = WorldToCell(worldPosition);
            if (cell != _highlightedCell)
            {
                _highlightedCell = cell;
            }
        }

        public void ClearHighlight()
        {
            _highlightedCell = new Vector2Int(-1, -1);
        }

        // ─── Editor Gizmos ──────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showGridGizmos) return;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            for (int x = 0; x < _columns; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    Vector3 cellCenter;
                    Vector3 cellSize;

                    if (TargetSpace == DragSpace.World3D)
                    {
                        cellCenter = _gridOrigin + new Vector3(
                            (x + 0.5f) * _cellSize.x, 0, (y + 0.5f) * _cellSize.y
                        );
                        cellSize = new Vector3(_cellSize.x, 0.01f, _cellSize.y);
                    }
                    else
                    {
                        cellCenter = _gridOrigin + new Vector3(
                            (x + 0.5f) * _cellSize.x, (y + 0.5f) * _cellSize.y, 0
                        );
                        cellSize = new Vector3(_cellSize.x, _cellSize.y, 0.01f);
                    }

                    Vector2Int cell = new Vector2Int(x, y);

                    if (cell == _highlightedCell)
                        Gizmos.color = _highlightGizmoColor;
                    else if (_occupiedCells != null && IsCellOccupied(cell))
                        Gizmos.color = _occupiedGizmoColor;
                    else
                        Gizmos.color = _gridGizmoColor;

                    Gizmos.DrawWireCube(cellCenter, cellSize * 0.95f);
                    Gizmos.DrawCube(cellCenter, cellSize * 0.95f);
                }
            }

            Gizmos.matrix = oldMatrix;
        }
#endif
    }
}
