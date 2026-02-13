using UnityEngine;
using System.Collections.Generic;
using JellyGunner;

public class BlockTargetManager : MonoBehaviour
{
    public static BlockTargetManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private BlockConfigSO _blockConfig;
    [SerializeField] private Vector2 _spacing = new Vector2(1.1f, 1.1f);
    [SerializeField] private Transform _container;

    // Grid: Key = (x, y, layer), Value = Block occupying that cell
    private Dictionary<Vector3Int, TargetBlock> _grid = new Dictionary<Vector3Int, TargetBlock>();
    private List<TargetBlock> _activeTargets = new List<TargetBlock>();

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(LevelData levelData)
    {
        ClearLevel();
        if (levelData == null || levelData.waves == null) return;

        if (levelData.waves.Length == 0) return;
        var wave = levelData.waves[0];
        if (wave.enemies == null) return;

        var heightMap = new Dictionary<Vector2Int, int>();

        foreach (var spawn in wave.enemies)
        {
            var config = _blockConfig.GetDefinition(spawn.color);
            if (config == null) continue;

            int startX = spawn.gridX;
            int startY = spawn.gridY;
            int currentLayer = 0;

            for (int x = 0; x < config.Size.x; x++)
            {
                for (int y = 0; y < config.Size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(startX + x, startY + y);
                    if (heightMap.TryGetValue(pos, out int h))
                    {
                        if (h > currentLayer) currentLayer = h;
                    }
                }
            }

            SpawnBlock(config, startX, startY, currentLayer);

            int nextLayer = currentLayer + 1;
            for (int x = 0; x < config.Size.x; x++)
            {
                for (int y = 0; y < config.Size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(startX + x, startY + y);
                    heightMap[pos] = nextLayer;
                }
            }
        }
    }

    private void SpawnBlock(BlockConfigSO.BlockDefinition config, int x, int y, int layer)
    {
        Vector3 worldPos = CalculateWorldPosition(x, y, layer, config.Size);
        GameObject obj = Instantiate(config.Prefab, worldPos, Quaternion.identity, _container);
        
        TargetBlock block = obj.GetComponent<TargetBlock>();
        if (block == null) block = obj.AddComponent<TargetBlock>();

        block.Initialize(config, x, y, layer, OnBlockDestroyed);
        _activeTargets.Add(block);

        for (int i = 0; i < config.Size.x; i++)
        {
            for (int j = 0; j < config.Size.y; j++)
            {
                _grid[new Vector3Int(x + i, y + j, layer)] = block;
            }
        }
    }

    private void OnBlockDestroyed(TargetBlock block)
    {
        if (_activeTargets.Contains(block))
        {
            _activeTargets.Remove(block);
            
            var keysToRemove = new List<Vector3Int>();
            foreach (var kvp in _grid)
            {
                if (kvp.Value == block) keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove) _grid.Remove(k);
        }
    }

    public TargetBlock GetTarget(BlockColor color)
    {
        TargetBlock candidate = null;
        int minLayer = int.MaxValue;

        foreach (var target in _activeTargets)
        {
            if (target.ColorType == color && target.HP > 0)
            {
                if (target.LayerIndex < minLayer)
                {
                    minLayer = target.LayerIndex;
                    candidate = target;
                }
            }
        }
        return candidate;
    }

    private Vector3 CalculateWorldPosition(int x, int y, int layer, Vector2Int size)
    {
        float xPos = (x + (size.x - 1) * 0.5f) * _spacing.x;
        float zPos = (y + (size.y - 1) * 0.5f) * _spacing.y;
        float yPos = layer * 1.0f;

        return transform.position + new Vector3(xPos, yPos, zPos);
    }

    private void ClearLevel()
    {
        foreach (var t in _activeTargets)
        {
            if (t != null) Destroy(t.gameObject);
        }
        _activeTargets.Clear();
        _grid.Clear();
    }
}
