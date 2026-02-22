using UnityEngine;
using System.Collections.Generic;
using JellyGunner;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GameObject SupplyPrefab;
    [SerializeField] private LayoutManager _layoutManager;
    [SerializeField] private TrayManager _trayManager;
    [SerializeField] private BlockTargetManager _targetManager;

    [Header("Level Settings")]
    [SerializeField] private LevelData _levelData;
    [SerializeField] private BlockConfigSO _blockConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        if (_levelData == null)
        {
            Debug.LogError("GameManager: LevelData is missing!");
            return;
        }

        // 1. Initialize Targets (Enemy Layout)
        if (_targetManager != null)
        {
            // Use random fill logic instead of LevelData for now
            _targetManager.InitializeRandom();
        }
        else
        {
            Debug.LogError("GameManager: BlockTargetManager is missing!");
        }

        // 2. Initialize Supply (Ammo Layout)
        if (_layoutManager != null)
        {
            // Check waves
            if (_levelData.waves == null || _levelData.waves.Length == 0)
            {
                Debug.LogWarning("GameManager: No waves in LevelData!");
                return;
            }

            var supplies = new List<SupplyData>();
            var waveSupplies = _levelData.waves[0].supply;

            if (waveSupplies != null)
            {
                int idCounter = 0;
                foreach (var entry in waveSupplies)
                {
                    var config = _blockConfig != null ? _blockConfig.GetDefinition(entry.color) : null;
                    Color visualColor = config != null ? config.VisualColor : Color.white;

                    supplies.Add(new SupplyData
                    {
                        ID = idCounter++,
                        Amount = entry.Ammo,
                        BaseColor = visualColor,
                        ColorEnum = entry.color
                    });
                }
            }

            _layoutManager.InitializeGrid(supplies);
        }
        else
        {
            Debug.LogError("GameManager: LayoutManager is missing!");
        }
    }

    public void ProcessSupplySelection(SupplyItem item)
    {
        if (_trayManager == null || _layoutManager == null) return;

        if (!_trayManager.HasSpace())
        {
            ShakeTrayEffect();
            return;
        }

        _layoutManager.RemoveItemFromGrid(item);
        _trayManager.AddSupply(item);
    }

    private void ShakeTrayEffect()
    {
        if (_trayManager == null) return;

        LeanTween.cancel(_trayManager.gameObject);
        LeanTween.moveLocalX(_trayManager.gameObject, 0.1f, 0.05f)
            .setEaseShake()
            .setLoopPingPong(2);
    }
}
