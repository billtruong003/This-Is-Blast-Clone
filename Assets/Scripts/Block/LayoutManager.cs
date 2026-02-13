using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class LayoutManager : MonoBehaviour
{
    [Header("1. Boundary & Anchor")]
    [Tooltip("Uses Unity's standard UI Anchor logic mapped to 3D space (XZ Plane).")]
    [SerializeField] private TextAnchor _anchor = TextAnchor.UpperCenter;

    [Header("2. Grid Settings")]
    [SerializeField] private int _maxColumns = 3;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.5f);

    [Header("3. Debug")]
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _previewColor = new Color(0, 1, 1, 0.5f);
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField, Range(0, 50)] private int _previewCount = 10;

    private BoxCollider _boundary;
    private readonly List<SupplyItem> _items = new List<SupplyItem>();

    private void Awake()
    {
        _boundary = GetComponent<BoxCollider>();
        _boundary.isTrigger = true; // Ensure it doesn't collide physically
    }

    private void OnValidate()
    {
        if (_boundary == null) _boundary = GetComponent<BoxCollider>();
    }

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------

    public void InitializeGrid(List<SupplyData> supplies)
    {
        ClearGrid();

        for (int i = 0; i < supplies.Count; i++)
        {
            // Determine Column and Row based on initial index
            int col = i % _maxColumns;
            int row = i / _maxColumns;

            Vector3 pos = CalculatePosition(col, row);

            var obj = ObjectPoolManager.Instance.Spawn(GameManager.Instance.SupplyPrefab, pos, Quaternion.identity, transform);
            var item = obj.GetComponent<SupplyItem>();

            item.Setup(supplies[i], col, row, OnItemClicked);
            _items.Add(item);
        }
    }

    public void RemoveItemFromGrid(SupplyItem item)
    {
        if (_items.Contains(item))
        {
            _items.Remove(item);
            ReorderGrid();
        }
    }

    // ---------------------------------------------------------
    // CORE LOGIC (Vertical Stack Logic)
    // ---------------------------------------------------------

    private void ReorderGrid()
    {
        // Group items by column to process vertical gravity independently
        // Dictionary key: Column Index, Value: List of items in that column
        var columns = new Dictionary<int, List<SupplyItem>>();

        foreach (var item in _items)
        {
            if (!columns.ContainsKey(item.ColumnIndex))
            {
                columns[item.ColumnIndex] = new List<SupplyItem>();
            }
            columns[item.ColumnIndex].Add(item);
        }

        // Process each column
        foreach (var kvp in columns)
        {
            int colIndex = kvp.Key;
            List<SupplyItem> columnItems = kvp.Value;

            // Sort items by their current Row Index to maintain relative order
            // Assuming we stack from bottom (row 0) to top (row N)
            columnItems.Sort((a, b) => a.RowIndex.CompareTo(b.RowIndex));

            // Re-assign Row Indices sequentially starting from 0 (bottom)
            for (int r = 0; r < columnItems.Count; r++)
            {
                var item = columnItems[r];
                int newRow = r;

                // Only update if position changed
                if (item.RowIndex != newRow)
                {
                    Vector3 targetPos = CalculatePosition(colIndex, newRow);
                    item.UpdateGridPosition(newRow, targetPos);
                }
            }
        }
    }

    private void OnItemClicked(SupplyItem item)
    {
        // Simple logic: Only pick if it's the "last" visually in the stack?
        // Let's assume standard behavior: Can pick any available.
        GameManager.Instance.ProcessSupplySelection(item);
    }

    private void ClearGrid()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] != null) _items[i].gameObject.Despawn();
        }
        _items.Clear();
    }

    /// <summary>
    /// Calculates world position given a specific Column and Row.
    /// </summary>
    private Vector3 CalculatePosition(int col, int row)
    {
        if (_boundary == null) _boundary = GetComponent<BoxCollider>();

        // Calculate grid dimensions relative to the max columns. 
        // Note: For vertical centering (Middle Anchors), we might need total rows, 
        // but strictly per-item calculation is usually based on fixed start point.
        float gridWidth = (_maxColumns - 1) * _spacing.x;

        // For Middle Anchors, we ideally need to know Total Rows to center it.
        // However, for a dynamic "Tetris-like" stack, we usually anchor to Bottom or Top.
        // If anchoring Middle, it implies the WHOLE grid centers. For now, let's assume 
        // Middle anchors center based on current max items, or just treat Row 0 as center.
        // To keep it simple and consistent with "Push Up" logic, we'll calculate relative to anchor.

        Vector3 anchorOrigin = GetAnchorLocalPosition();
        Vector3 startOffset = Vector3.zero;
        Vector3 growthDir = Vector3.zero;

        // X Axis Logic
        switch (_anchor)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                startOffset.x = 0;
                growthDir.x = 1;
                break;

            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                startOffset.x = -gridWidth * 0.5f;
                growthDir.x = 1;
                break;

            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                startOffset.x = 0;
                growthDir.x = -1;
                break;
        }

        // Z Axis Logic (Vertical in 3D Top-Down)
        switch (_anchor)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                startOffset.z = 0;
                growthDir.z = -1; // Grow Down
                break;

            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                // Middle alignment for dynamic stacks is tricky. 
                // Let's assume Row 0 starts at center and grows down/up? 
                // Or better: Treat Middle as "Center of Grid"? 
                // For stable "Stacking", Middle usually means Center of First Row, growing down? 
                // Let's stick to standard UI logic: Middle means (0,0) is center, 
                // but usually requires knowing total height. 
                // Simplified: Start at center, grow down.
                startOffset.z = 0;
                growthDir.z = -1;
                break;

            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                startOffset.z = 0;
                growthDir.z = 1; // Grow Up
                break;
        }

        Vector3 localItemPos = startOffset + new Vector3(
            col * _spacing.x * growthDir.x,
            0,
            row * _spacing.y * growthDir.z
        );

        Vector3 worldAnchorPos = transform.TransformPoint(anchorOrigin);
        return worldAnchorPos + (transform.rotation * localItemPos);
    }

    private Vector3 GetAnchorLocalPosition()
    {
        Vector3 center = _boundary.center;
        Vector3 extents = _boundary.size * 0.5f;

        float x = center.x;
        float z = center.z;

        // X Alignment
        if (_anchor.ToString().Contains("Left")) x -= extents.x;
        else if (_anchor.ToString().Contains("Right")) x += extents.x;

        // Z Alignment (Upper = Z+, Lower = Z-)
        if (_anchor.ToString().Contains("Upper")) z += extents.z;
        else if (_anchor.ToString().Contains("Lower")) z -= extents.z;

        // Assume Y is on the floor of the collider (or center if you prefer)
        // Let's keep Y at center + offset? usually layouts are flat on the object.
        // Let's use center.y
        float y = center.y;

        return new Vector3(x, y, z);
    }

    // ---------------------------------------------------------
    // DEBUG GIZMOS
    // ---------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        // Ensure we have reference
        if (_boundary == null) _boundary = GetComponent<BoxCollider>();
        if (_boundary == null) return;

        // 1. Draw Boundary
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_boundary.center, _boundary.size);

        // 2. Draw Anchor Point
        Vector3 anchorLocal = GetAnchorLocalPosition();
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(anchorLocal, 0.3f);
        Handles.Label(transform.TransformPoint(anchorLocal) + Vector3.up, _anchor.ToString());

        // 3. Draw Preview Items
        if (_previewCount > 0)
        {
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix for world space calculation
            Gizmos.color = _previewColor;
            
            Mesh mesh = null;
            if (_previewPrefab != null) mesh = _previewPrefab.GetComponent<MeshFilter>()?.sharedMesh;

            for (int i = 0; i < _previewCount; i++)
            {
                int col = i % _maxColumns;
                int row = i / _maxColumns;
                Vector3 pos = CalculatePosition(col, row);
                
                if (mesh != null)
                    Gizmos.DrawWireMesh(mesh, pos, Quaternion.identity, Vector3.one);
                else
                    Gizmos.DrawWireCube(pos, new Vector3(1f, 0.2f, 1f));

                if (i == 0)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.TransformPoint(anchorLocal), pos);
                    Gizmos.color = _previewColor;
                }
            }
        }
    }
#endif
}