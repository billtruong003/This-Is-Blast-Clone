#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace JellyGunner.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelData _target;
        private int _editingWave;
        private BlockColor _paintColor = BlockColor.Red;
        private EnemyTier _paintTier = EnemyTier.Standard;
        private int _gridColumns = 7;
        private int _gridRows = 10;
        private int _traySlots = 5;
        private int _supplyColumns = 4;
        private int _hammerCharges = 1;
        private Vector2 _scrollPos;

        private readonly Dictionary<Vector2Int, CellData> _cells = new();

        private struct CellData
        {
            public BlockColor Color;
            public EnemyTier Tier;
        }

        private static readonly Color[] ColorMap =
        {
            new Color(0.95f, 0.25f, 0.3f),
            new Color(0.2f, 0.5f, 0.95f),
            new Color(0.25f, 0.9f, 0.4f),
            new Color(1f, 0.85f, 0.2f)
        };

        private static readonly string[] ColorNames = { "Red", "Blue", "Green", "Yellow" };
        private static readonly string[] TierNames = { "Tiny (1 HP)", "Standard (20 HP)", "Medium (60 HP)", "Tank (120 HP)" };

        [MenuItem("JellyGunner/Level Editor")]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>("Level Editor").minSize = new Vector2(600, 700);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("JELLY GUNNER - LEVEL EDITOR", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawTargetSection();
            DrawGridSettings();
            DrawPalette();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawGrid();
            EditorGUILayout.EndScrollView();

            DrawStats();
            DrawActions();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.BeginHorizontal();
            _target = (LevelData)EditorGUILayout.ObjectField("Target Level", _target, typeof(LevelData), false);

            if (GUILayout.Button("New", GUILayout.Width(50)))
                CreateNewLevelAsset();

            if (_target && GUILayout.Button("Load", GUILayout.Width(50)))
                LoadFromTarget();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            _gridColumns = EditorGUILayout.IntSlider("Columns", _gridColumns, 3, 12);
            _gridRows = EditorGUILayout.IntSlider("Rows", _gridRows, 3, 20);
            _traySlots = EditorGUILayout.IntSlider("Tray Slots", _traySlots, 3, 5);
            _supplyColumns = EditorGUILayout.IntSlider("Supply Columns", _supplyColumns, 3, 6);
            _hammerCharges = EditorGUILayout.IntSlider("Hammer Charges", _hammerCharges, 0, 3);
        }

        private void DrawPalette()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Paint Palette", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 4; i++)
            {
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = _paintColor == (BlockColor)i
                    ? ColorMap[i] * 1.5f
                    : ColorMap[i] * 0.6f;

                if (GUILayout.Button(ColorNames[i], GUILayout.Height(30)))
                    _paintColor = (BlockColor)i;

                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 4; i++)
            {
                var style = _paintTier == (EnemyTier)i ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(TierNames[i], style, GUILayout.Height(24)))
                    _paintTier = (EnemyTier)i;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Height(24)))
                _cells.Clear();
            if (GUILayout.Button("Fill Row 0", GUILayout.Height(24)))
                FillRow(0);
            if (GUILayout.Button("Randomize", GUILayout.Height(24)))
                RandomFill();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.Space(8);
            float cellSize = Mathf.Min(40f, (position.width - 40f) / _gridColumns);

            for (int y = _gridRows - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int x = 0; x < _gridColumns; x++)
                {
                    var key = new Vector2Int(x, y);
                    bool hasCell = _cells.TryGetValue(key, out var cell);

                    var prevBg = GUI.backgroundColor;

                    if (hasCell)
                    {
                        GUI.backgroundColor = ColorMap[(int)cell.Color];
                        string label = TierConfig.GetHP(cell.Tier).ToString();

                        if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                        {
                            if (Event.current.button == 1)
                                _cells.Remove(key);
                            else
                                _cells[key] = new CellData { Color = _paintColor, Tier = _paintTier };
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                        if (GUILayout.Button("", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                            _cells[key] = new CellData { Color = _paintColor, Tier = _paintTier };
                    }

                    GUI.backgroundColor = prevBg;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStats()
        {
            EditorGUILayout.Space(8);

            int totalHP = 0;
            int[] colorHP = new int[4];
            int[] colorCount = new int[4];

            foreach (var kvp in _cells)
            {
                int hp = TierConfig.GetHP(kvp.Value.Tier);
                totalHP += hp;
                colorHP[(int)kvp.Value.Color] += hp;
                colorCount[(int)kvp.Value.Color]++;
            }

            EditorGUILayout.LabelField($"Enemies: {_cells.Count}  |  Total HP: {totalHP}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 4; i++)
            {
                if (colorCount[i] == 0) continue;
                EditorGUILayout.LabelField($"{ColorNames[i]}: {colorCount[i]} ({colorHP[i]} HP)");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                $"Supply needed: {totalHP} total ammo (must match Total HP for perfect balance)",
                MessageType.Info
            );
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _target != null && _cells.Count > 0;
            if (GUILayout.Button("SAVE TO LEVEL", GUILayout.Height(35)))
                SaveToTarget();
            GUI.enabled = true;

            if (GUILayout.Button("Auto-Generate Supply", GUILayout.Height(35)))
                AutoGenerateAndSave();

            EditorGUILayout.EndHorizontal();
        }

        private void SaveToTarget()
        {
            if (_target == null) return;

            Undo.RecordObject(_target, "Save Level Grid");

            _target.columns = _gridColumns;
            _target.rows = _gridRows;
            _target.traySlots = _traySlots;
            _target.supplyColumns = _supplyColumns;
            _target.hammerCharges = _hammerCharges;

            var spawns = new List<LevelData.EnemySpawn>();
            foreach (var kvp in _cells)
            {
                spawns.Add(new LevelData.EnemySpawn
                {
                    gridX = kvp.Key.x,
                    gridY = kvp.Key.y,
                    color = kvp.Value.Color,
                    tier = kvp.Value.Tier
                });
            }

            if (_target.waves == null || _target.waves.Length == 0)
                _target.waves = new LevelData.WaveData[1];

            _target.waves[0].enemies = spawns.ToArray();
            _target.waves[0].advanceSpeed = 0.03f;

            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }

        private void AutoGenerateAndSave()
        {
            SaveToTarget();
            if (_target == null) return;

            int[] colorAmmoNeeded = new int[4];
            foreach (var kvp in _cells)
                colorAmmoNeeded[(int)kvp.Value.Color] += TierConfig.GetHP(kvp.Value.Tier);

            var supply = new List<LevelData.SupplyEntry>();

            for (int c = 0; c < 4; c++)
            {
                int remaining = colorAmmoNeeded[c];
                if (remaining <= 0) continue;

                while (remaining > 0)
                {
                    BlasterType type;
                    int ammo;

                    if (remaining >= 120)
                    {
                        type = BlasterType.Gatling;
                        ammo = TierConfig.GetAmmo(type);
                    }
                    else if (remaining >= 60)
                    {
                        type = BlasterType.Sniper;
                        ammo = TierConfig.GetAmmo(type);
                    }
                    else
                    {
                        type = BlasterType.Pistol;
                        ammo = TierConfig.GetAmmo(type);
                    }

                    supply.Add(new LevelData.SupplyEntry
                    {
                        color = (BlockColor)c,
                        type = type
                    });

                    remaining -= ammo;
                }
            }

            ShuffleList(supply);
            _target.waves[0].supply = supply.ToArray();

            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }

        private void LoadFromTarget()
        {
            if (_target == null) return;

            _gridColumns = _target.columns;
            _gridRows = _target.rows;
            _traySlots = _target.traySlots;
            _supplyColumns = _target.supplyColumns;
            _hammerCharges = _target.hammerCharges;
            _cells.Clear();

            if (_target.waves == null || _target.waves.Length == 0) return;

            var enemies = _target.waves[0].enemies;
            if (enemies == null) return;

            foreach (var e in enemies)
                _cells[new Vector2Int(e.gridX, e.gridY)] = new CellData { Color = e.color, Tier = e.tier };
        }

        private void FillRow(int row)
        {
            for (int x = 0; x < _gridColumns; x++)
                _cells[new Vector2Int(x, row)] = new CellData { Color = _paintColor, Tier = _paintTier };
        }

        private void RandomFill()
        {
            _cells.Clear();
            for (int y = 0; y < _gridRows; y++)
            {
                for (int x = 0; x < _gridColumns; x++)
                {
                    if (Random.value > 0.7f) continue;
                    _cells[new Vector2Int(x, y)] = new CellData
                    {
                        Color = (BlockColor)Random.Range(0, (int)BlockColor.Count),
                        Tier = (EnemyTier)Random.Range(0, 2)
                    };
                }
            }
        }

        private void CreateNewLevelAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level Data", "NewLevel", "asset", "Choose save location"
            );
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _target = asset;
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
#endif
