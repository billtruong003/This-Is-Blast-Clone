using UnityEngine;
using UnityEditor;
using ImageToVoxel.Game;

namespace ImageToVoxel
{
    public class GameSetupWizard : EditorWindow
    {
        private LevelData initialLevel;
        private GameObject blockPrefab;
        private GameObject blastPrefab;
        private GameObject projectilePrefab;
        private float cellSize = 1f;
        private bool createCamera = true;
        private bool createUI = true;

        [MenuItem("Tools/Image To Voxel/Game Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<GameSetupWizard>("Blast Game Setup").minSize = new Vector2(350, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("This is Blast - Game Setup", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.Space(8);

            DrawSeparator();

            EditorGUILayout.LabelField("Required References", EditorStyles.boldLabel);
            initialLevel = (LevelData)EditorGUILayout.ObjectField("Initial Level", initialLevel, typeof(LevelData), false);
            blockPrefab = (GameObject)EditorGUILayout.ObjectField("Block Prefab", blockPrefab, typeof(GameObject), false);
            blastPrefab = (GameObject)EditorGUILayout.ObjectField("Blast Prefab", blastPrefab, typeof(GameObject), false);
            projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", projectilePrefab, typeof(GameObject), false);

            DrawSeparator();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            createCamera = EditorGUILayout.Toggle("Create Game Camera", createCamera);
            createUI = EditorGUILayout.Toggle("Create UI Canvas", createUI);

            DrawSeparator();

            EditorGUILayout.Space(8);

            if (blockPrefab == null || blastPrefab == null)
                EditorGUILayout.HelpBox("Block and Blast prefabs are required to set up the game.", MessageType.Warning);

            EditorGUI.BeginDisabledGroup(blockPrefab == null || blastPrefab == null);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Create Game Scene", GUILayout.Height(40)))
                CreateGameScene();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "This will create:\n" +
                "• GameManager (state machine)\n" +
                "• GridManager (grid logic)\n" +
                "• MapGenerator (level gen from SO)\n" +
                "• BlastTray (player's blast inventory)\n" +
                "• DragDropController (input handling)\n" +
                "• UIManager (HUD + screens)\n" +
                "• Camera (top-down view)",
                MessageType.Info);
        }

        private void CreateGameScene()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            var root = CreateRoot();
            var gameManager = SetupGameManager(root);
            var gridManager = SetupGridManager(root);
            var mapGenerator = SetupMapGenerator(root, gridManager);
            var blastTray = SetupBlastTray(root);
            var dragDrop = SetupDragDrop(root, gridManager, blastTray);
            var uiManager = createUI ? SetupUI(root) : null;

            if (createCamera)
                SetupCamera(root);

            WireReferences(gameManager, gridManager, mapGenerator, blastTray, dragDrop, uiManager);

            Undo.SetCurrentGroupName("Create Blast Game Scene");
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private GameObject CreateRoot()
        {
            var root = new GameObject("[BLAST GAME]");
            Undo.RegisterCreatedObjectUndo(root, "Create Game Root");
            return root;
        }

        private GameManager SetupGameManager(GameObject root)
        {
            var obj = CreateChild(root, "GameManager");
            return obj.AddComponent<GameManager>();
        }

        private GridManager SetupGridManager(GameObject root)
        {
            var obj = CreateChild(root, "GridManager");
            var gm = obj.AddComponent<GridManager>();
            return gm;
        }

        private MapGenerator SetupMapGenerator(GameObject root, GridManager gridManager)
        {
            var obj = CreateChild(root, "MapGenerator");
            var mg = obj.AddComponent<MapGenerator>();

            var so = new SerializedObject(mg);
            so.FindProperty("cellSize").floatValue = cellSize;
            so.FindProperty("centerGrid").boolValue = true;
            so.FindProperty("adjustCountsToDivisible").boolValue = true;
            so.FindProperty("blocksPerBlast").intValue = 20;

            if (initialLevel != null)
            {
                so.FindProperty("levelData").objectReferenceValue = initialLevel;
                if (initialLevel.MapData != null)
                {
                    so.FindProperty("mapData").objectReferenceValue = initialLevel.MapData;
                    var prefabsArray = so.FindProperty("rangePrefabs");
                    prefabsArray.arraySize = initialLevel.MapData.TotalRanges;
                    for (int i = 0; i < prefabsArray.arraySize; i++)
                        prefabsArray.GetArrayElementAtIndex(i).objectReferenceValue = blockPrefab;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return mg;
        }

        private BlastTray SetupBlastTray(GameObject root)
        {
            var obj = CreateChild(root, "BlastTray");
            obj.transform.localPosition = new Vector3(0, 0, -8);
            var tray = obj.AddComponent<BlastTray>();

            var so = new SerializedObject(tray);
            so.FindProperty("blastPrefab").objectReferenceValue = blastPrefab;
            if (projectilePrefab != null)
                so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            return tray;
        }

        private DragDropController SetupDragDrop(GameObject root, GridManager gridManager, BlastTray tray)
        {
            var obj = CreateChild(root, "DragDropController");
            var dd = obj.AddComponent<DragDropController>();

            var so = new SerializedObject(dd);
            so.FindProperty("gridManager").objectReferenceValue = gridManager;
            so.FindProperty("blastTray").objectReferenceValue = tray;
            so.ApplyModifiedPropertiesWithoutUndo();

            return dd;
        }

        private UIManager SetupUI(GameObject root)
        {
            var canvasObj = CreateChild(root, "UICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var uiManager = canvasObj.AddComponent<UIManager>();

            CreatePanel(canvasObj.transform, "HUDPanel", new Vector2(0, 1), new Vector2(0, 1));
            CreatePanel(canvasObj.transform, "LevelCompletePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            CreatePanel(canvasObj.transform, "GameOverPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            CreatePanel(canvasObj.transform, "PausePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            return uiManager;
        }

        private void SetupCamera(GameObject root)
        {
            var camObj = CreateChild(root, "GameCamera");
            camObj.transform.position = new Vector3(0, 15, -8);
            camObj.transform.rotation = Quaternion.Euler(60, 0, 0);

            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.orthographic = false;
            cam.fieldOfView = 45;
            cam.tag = "MainCamera";

            camObj.AddComponent<AudioListener>();
        }

        private void WireReferences(GameManager gm, GridManager grid, MapGenerator map, BlastTray tray, DragDropController dd, UIManager ui)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("gridManager").objectReferenceValue = grid;
            so.FindProperty("blastTray").objectReferenceValue = tray;
            so.FindProperty("dragDropController").objectReferenceValue = dd;

            var levelMgrObj = CreateChild(gm.gameObject, "LevelManager");
            var lm = levelMgrObj.AddComponent<LevelManager>();

            var lmSo = new SerializedObject(lm);
            lmSo.FindProperty("gridManager").objectReferenceValue = grid;
            lmSo.FindProperty("blastTray").objectReferenceValue = tray;
            lmSo.FindProperty("mapGenerator").objectReferenceValue = map;
            lmSo.ApplyModifiedPropertiesWithoutUndo();

            so.FindProperty("levelManager").objectReferenceValue = lm;
            if (ui != null)
                so.FindProperty("uiManager").objectReferenceValue = ui;

            if (initialLevel != null)
            {
                var levelsArray = so.FindProperty("levels");
                levelsArray.arraySize = 1;
                levelsArray.GetArrayElementAtIndex(0).objectReferenceValue = initialLevel;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            return child;
        }

        private static void CreatePanel(Transform canvasTransform, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(canvasTransform);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = new Vector2(300, 200);
            rt.anchoredPosition = Vector2.zero;

            panel.SetActive(false);
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(2);
        }
    }
}
