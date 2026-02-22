using UnityEngine;
using System.Collections.Generic;
using JellyGunner;

public class BlockTargetManager : MonoBehaviour
{
    public static BlockTargetManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private BlockConfigSO _blockConfig;
    [SerializeField] private Transform _container;

    [Header("Grid Layout Settings")]
    [Header("Spawn Settings")]
    [Tooltip("Size of the grid to spawn (Columns x Depth)")]
    [SerializeField] private Vector2Int _gridSize = new Vector2Int(5, 5);

    [Tooltip("How to align the grid relative to this object's position")]
    [SerializeField] private TextAnchor _anchor = TextAnchor.LowerCenter;

    [Tooltip("Additional manual offset if needed")]
    [SerializeField] private Vector3 _manualOffset = Vector3.zero;

    [Tooltip("Distance between columns and rows")]
    [SerializeField] private Vector2 _spacing = new Vector2(1.2f, 1.2f);

    [Tooltip("Global scale multiplier for blocks (Separated from Spacing)")]
    [SerializeField] private float _blockScaleMultiplier = 1.0f;

    [Tooltip("Direction the blocks slide towards (e.g., (0,0,-1) means sliding back-to-front along Z)")]
    [SerializeField] private Vector3 _slideDirection = new Vector3(0, 0, -1);

    [Tooltip("Axis defining the Columns (Perpendicular to Slide). Usually (1,0,0)")]
    [SerializeField] private Vector3 _columnAxis = new Vector3(1, 0, 0);

    [Header("Debug")]
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _gizmoColor = Color.red;

    // Key: Column Index, Value: List of blocks in that column sorted by Depth (0 is closest to pivot)
    private Dictionary<int, List<TargetBlock>> _columns = new Dictionary<int, List<TargetBlock>>();
    private List<TargetBlock> _allTargets = new List<TargetBlock>();

    private void Awake()
    {
        Instance = this;
        if (_slideDirection == Vector3.zero) _slideDirection = Vector3.back;
        _slideDirection.Normalize();
        _columnAxis.Normalize();
    }

    public void InitializeRandom()
    {
        ClearLevel();

        for (int c = 0; c < _gridSize.x; c++)
        {
            _columns[c] = new List<TargetBlock>();

            for (int d = 0; d < _gridSize.y; d++)
            {
                var config = _blockConfig.GetRandomDefinition();
                if (config == null) continue;

                SpawnBlock(config, c, d);
            }
        }
    }

    private void SpawnBlock(BlockConfigSO.BlockDefinition config, int col, int depth)
    {
        // Calculate center position for multi-cell blocks
        // The base position is the bottom-left cell (col, depth)
        // We need to shift it by (Size - 1) * 0.5 * Spacing
        Vector3 basePos = CalculateWorldPosition(col, depth);

        Vector3 sizeOffset = Vector3.zero;
        // Shift along Column Axis
        sizeOffset += _columnAxis * ((config.Size.x - 1) * 0.5f * _spacing.x);
        // Shift along Slide Axis (backwards from slide direction)
        sizeOffset += -_slideDirection * ((config.Size.y - 1) * 0.5f * _spacing.y);

        Vector3 finalPos = basePos + sizeOffset;

        GameObject obj = Instantiate(config.Prefab, finalPos, Quaternion.identity, _container);

        // Apply Scale INDEPENDENTLY from Spacing
        // Scale = BlockSize * Multiplier
        Vector3 newScale = obj.transform.localScale;

        // Scale along X (Column Axis)
        newScale.x = config.Size.x * _blockScaleMultiplier;
        // Scale along Z (Slide Axis)
        newScale.z = config.Size.y * _blockScaleMultiplier;

        // Y Axis usually remains 1 or scales proportionally? Keeping it as is or applying multiplier
        // newScale.y *= _blockScaleMultiplier; // Optional if you want uniform scaling on Y too

        obj.transform.localScale = newScale;

        TargetBlock block = obj.GetComponent<TargetBlock>();
        if (block == null) block = obj.AddComponent<TargetBlock>();

        block.Initialize(config, col, depth, OnBlockDestroyed);

        if (!_columns.ContainsKey(col)) _columns[col] = new List<TargetBlock>();
        _columns[col].Add(block);
        _allTargets.Add(block);
    }

    private void OnBlockDestroyed(TargetBlock block)
    {
        if (_allTargets.Contains(block)) _allTargets.Remove(block);

        int col = block.ColumnIndex;
        if (_columns.ContainsKey(col))
        {
            _columns[col].Remove(block);
            SlideColumn(col);
        }
    }

    private void SlideColumn(int colIndex)
    {
        if (!_columns.ContainsKey(colIndex)) return;

        var list = _columns[colIndex];

        // Re-index depth from 0 to Count
        for (int i = 0; i < list.Count; i++)
        {
            var targetBlock = list[i];

            // If the block is not at the correct visual depth index, move it
            if (targetBlock.DepthIndex != i)
            {
                Vector3 newPos = CalculateWorldPosition(colIndex, i);
                targetBlock.UpdateGridPosition(i, newPos);
            }
        }
    }

    public TargetBlock GetTarget(BlockColor color)
    {
        TargetBlock candidate = null;
        float minDistanceSqr = float.MaxValue;
        Vector3 origin = GetStartOrigin();

        // Check only the first item (Depth 0) of each column
        foreach (var kvp in _columns)
        {
            var list = kvp.Value;
            if (list.Count == 0) continue;

            TargetBlock frontBlock = list[0];

            if (frontBlock.ColorType == color && frontBlock.HP > 0)
            {
                // Find the one physically closest to the center/pivot if multiple exist
                float dist = (frontBlock.transform.position - origin).sqrMagnitude;
                if (dist < minDistanceSqr)
                {
                    minDistanceSqr = dist;
                    candidate = frontBlock;
                }
            }
        }
        return candidate;
    }

    private Vector3 CalculateWorldPosition(int col, int depth)
    {
        // Get the dynamic origin based on Anchor and Grid Size
        Vector3 origin = GetStartOrigin();

        // Move along Column Axis
        Vector3 colOffset = _columnAxis * (col * _spacing.x);

        // Move along Slide Axis (backwards from slide direction to stack them up)
        Vector3 depthOffset = -_slideDirection * (depth * _spacing.y);

        return origin + colOffset + depthOffset;
    }

    private Vector3 GetStartOrigin()
    {
        // Use inspector grid size for calculation
        int maxCol = _gridSize.x;
        int maxDepth = _gridSize.y;

        float totalWidth = (maxCol > 0 ? maxCol - 1 : 0) * _spacing.x;
        float totalDepth = (maxDepth > 0 ? maxDepth - 1 : 0) * _spacing.y;

        Vector3 alignmentOffset = Vector3.zero;

        // X Alignment (Column Axis)
        if (_anchor.ToString().Contains("Left"))
            alignmentOffset -= _columnAxis * 0;
        else if (_anchor.ToString().Contains("Right"))
            alignmentOffset -= _columnAxis * totalWidth;
        else // Center
            alignmentOffset -= _columnAxis * (totalWidth * 0.5f);

        // Z/Y Alignment (Slide Axis)
        // "Lower" means Front (Depth 0), "Upper" means Back (Max Depth)
        if (_anchor.ToString().Contains("Lower"))
            alignmentOffset -= -_slideDirection * 0;
        else if (_anchor.ToString().Contains("Upper"))
            alignmentOffset -= -_slideDirection * totalDepth;
        else // Middle
            alignmentOffset -= -_slideDirection * (totalDepth * 0.5f);

        return transform.position + alignmentOffset + _manualOffset;
    }



    private void ClearLevel()
    {
        foreach (var t in _allTargets)
        {
            if (t != null) Destroy(t.gameObject);
        }
        _allTargets.Clear();
        _columns.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        Vector3 origin = GetStartOrigin();
        int maxCol = _gridSize.x;
        int maxDepth = _gridSize.y;

        // 1. Draw Pivot/Origin Point
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(origin, 0.2f);
        UnityEditor.Handles.Label(origin + Vector3.up * 0.5f, $"Origin\n{_anchor}");

        // 2. Draw Directions
        Gizmos.color = Color.cyan; // Slide
        DrawArrow(origin, _slideDirection, 2f);

        Gizmos.color = Color.green; // Column
        DrawArrow(origin, _columnAxis, 2f);

        // 3. Draw Grid Boundary
        // Calculate corners of the whole grid for visualization
        Vector3 corner00 = CalculateWorldPosition(0, 0);
        Vector3 cornerX0 = CalculateWorldPosition(maxCol - 1, 0);
        Vector3 corner0Y = CalculateWorldPosition(0, maxDepth - 1);
        Vector3 cornerXY = CalculateWorldPosition(maxCol - 1, maxDepth - 1);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(corner00, cornerX0);
        Gizmos.DrawLine(corner00, corner0Y);
        Gizmos.DrawLine(cornerX0, cornerXY);
        Gizmos.DrawLine(corner0Y, cornerXY);

        // 4. Visualize Grid Slots (Preview)
        Gizmos.color = _gizmoColor;
        // Use actual Scale Multiplier for preview size
        Vector3 size = new Vector3(_blockScaleMultiplier, 0.2f, _blockScaleMultiplier);

        for (int c = 0; c < maxCol; c++)
        {
            for (int d = 0; d < maxDepth; d++)
            {
                Vector3 pos = CalculateWorldPosition(c, d);

                // Highlight depth 0
                if (d == 0)
                {
                    Color highlight = Color.red;
                    highlight.a = 0.5f;
                    Gizmos.color = highlight;
                    Gizmos.DrawCube(pos, size);
                    Gizmos.DrawWireCube(pos, size * 1.1f);
                }
                else
                {
                    Gizmos.color = _gizmoColor;
                    Gizmos.DrawWireCube(pos, size);
                }
            }
        }
    }

    private void DrawArrow(Vector3 pos, Vector3 dir, float length = 1f)
    {
        {
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Gizmos.DrawLine(pos + dir, pos + (dir - right) * 0.2f);
            Gizmos.DrawLine(pos + dir, pos + (dir + right) * 0.2f);
        }
#endif
    }
}
