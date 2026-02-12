using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private VoxelMapData mapData;
        [SerializeField] private LevelData levelData;
        [SerializeField] private GameObject[] rangePrefabs;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int blocksPerBlast = 20;
        [SerializeField] private bool centerGrid = true;
        [SerializeField] private bool adjustCountsToDivisible = true;

        private Transform gridParent;
        private List<GameObject> spawnedBlocks = new List<GameObject>();
        private Dictionary<int, List<Vector2Int>> adjustedPositions;

        public VoxelMapData MapData => mapData;
        public float CellSize => cellSize;
        public int BlocksPerBlast => blocksPerBlast;
        public Dictionary<int, List<Vector2Int>> AdjustedPositions => adjustedPositions;
        public bool HasGenerated => spawnedBlocks.Count > 0;

        public GridSnapshot Generate()
        {
            ClearGenerated();

            if (mapData == null || rangePrefabs == null || rangePrefabs.Length == 0)
                return null;

            gridParent = new GameObject("GeneratedGrid").transform;
            gridParent.SetParent(transform);
            gridParent.localPosition = Vector3.zero;

            adjustedPositions = BuildAdjustedPositions();
            var snapshot = new GridSnapshot(mapData.Width, mapData.Height);

            Vector3 offset = centerGrid
                ? new Vector3(-(mapData.Width - 1) * cellSize * 0.5f, 0, -(mapData.Height - 1) * cellSize * 0.5f)
                : Vector3.zero;

            foreach (var kvp in adjustedPositions)
            {
                int rangeIndex = kvp.Key;
                var positions = kvp.Value;
                GameObject prefab = GetPrefabForRange(rangeIndex);

                if (prefab == null) continue;

                Color color = levelData != null
                    ? levelData.GetColor(rangeIndex)
                    : GenerateFallbackColor(rangeIndex, mapData.TotalRanges);

                foreach (var pos in positions)
                {
                    Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize) + offset;
                    var instance = Instantiate(prefab, worldPos, Quaternion.identity, gridParent);
                    instance.name = $"Block_{rangeIndex}_{pos.x}_{pos.y}";

                    ApplyColor(instance, color);

                    var block = instance.GetComponent<Block>();
                    if (block == null)
                        block = instance.AddComponent<Block>();
                    block.Initialize(rangeIndex, pos);

                    spawnedBlocks.Add(instance);
                    snapshot.SetCell(pos.x, pos.y, rangeIndex);
                }
            }

            return snapshot;
        }

        public void ClearGenerated()
        {
            foreach (var obj in spawnedBlocks)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
            spawnedBlocks.Clear();

            if (gridParent != null)
                DestroyImmediate(gridParent.gameObject);
            gridParent = null;
            adjustedPositions = null;
        }

        public Dictionary<int, int> GetRawCounts()
        {
            if (mapData == null)
                return new Dictionary<int, int>();
            return mapData.CountPerRange();
        }

        public Dictionary<int, int> GetAdjustedCounts()
        {
            var raw = GetRawCounts();
            if (!adjustCountsToDivisible)
                return raw;

            var adjusted = new Dictionary<int, int>();
            foreach (var kvp in raw)
            {
                int remainder = kvp.Value % blocksPerBlast;
                adjusted[kvp.Key] = remainder == 0 ? kvp.Value : kvp.Value + (blocksPerBlast - remainder);
            }
            return adjusted;
        }

        private Dictionary<int, List<Vector2Int>> BuildAdjustedPositions()
        {
            var result = new Dictionary<int, List<Vector2Int>>();

            for (int i = 0; i < mapData.TotalRanges; i++)
                result[i] = new List<Vector2Int>(mapData.GetPositionsForRange(i));

            if (!adjustCountsToDivisible)
                return result;

            var adjustedCounts = GetAdjustedCounts();

            foreach (var kvp in adjustedCounts)
            {
                int rangeIndex = kvp.Key;
                int targetCount = kvp.Value;
                var positions = result[rangeIndex];

                if (positions.Count > targetCount)
                {
                    positions.RemoveRange(targetCount, positions.Count - targetCount);
                }
                else if (positions.Count < targetCount)
                {
                    int toAdd = targetCount - positions.Count;
                    var emptySlots = FindEmptyAdjacentSlots(result, mapData.Width, mapData.Height);
                    for (int i = 0; i < toAdd && i < emptySlots.Count; i++)
                        positions.Add(emptySlots[i]);
                }
            }

            return result;
        }

        private List<Vector2Int> FindEmptyAdjacentSlots(Dictionary<int, List<Vector2Int>> occupied, int width, int height)
        {
            var usedSet = new HashSet<Vector2Int>();
            foreach (var kvp in occupied)
                foreach (var pos in kvp.Value)
                    usedSet.Add(pos);

            var empty = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!usedSet.Contains(new Vector2Int(x, y)))
                        empty.Add(new Vector2Int(x, y));

            return empty;
        }

        private GameObject GetPrefabForRange(int rangeIndex)
        {
            if (rangePrefabs == null || rangeIndex < 0 || rangeIndex >= rangePrefabs.Length)
                return null;
            return rangePrefabs[rangeIndex];
        }

        private static void ApplyColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var material = new Material(renderer.sharedMaterial);
            material.color = color;
            renderer.material = material;
        }

        private static Color GenerateFallbackColor(int index, int total)
        {
            if (total <= 1) return Color.red;
            return Color.HSVToRGB((float)index / total, 0.75f, 0.9f);
        }
    }

    public class GridSnapshot
    {
        private readonly int[,] cells;

        public int Width { get; }
        public int Height { get; }

        public GridSnapshot(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new int[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    cells[x, y] = -1;
        }

        public void SetCell(int x, int y, int value)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                cells[x, y] = value;
        }

        public int GetCell(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return cells[x, y];
            return -1;
        }

        public bool IsEmpty(int x, int y)
        {
            return GetCell(x, y) == -1;
        }
    }
}
