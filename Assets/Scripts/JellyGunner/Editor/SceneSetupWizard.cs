#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

namespace JellyGunner.Editor
{
    public class SceneSetupWizard : EditorWindow
    {
        private GameConfig _config;
        private ColorPalette _palette;
        private LevelData _level;
        private Mesh _enemyMesh;
        private Material _enemyMaterial;
        private Mesh _projectileMesh;
        private Material _projectileMaterial;
        private ComputeShader _cullingShader;
        private BlasterDefinition[] _blasterDefs;
        private ParticleSystem _deathVFX;

        [MenuItem("JellyGunner/Scene Setup Wizard")]
        public static void Open()
        {
            GetWindow<SceneSetupWizard>("Scene Setup").minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("JELLY GUNNER - SCENE SETUP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign your assets below then click 'Build Scene' to create the complete game hierarchy.",
                MessageType.Info
            );
            EditorGUILayout.Space(8);

            _config = (GameConfig)EditorGUILayout.ObjectField("Game Config", _config, typeof(GameConfig), false);
            _palette = (ColorPalette)EditorGUILayout.ObjectField("Color Palette", _palette, typeof(ColorPalette), false);
            _level = (LevelData)EditorGUILayout.ObjectField("Level Data", _level, typeof(LevelData), false);
            _cullingShader = (ComputeShader)EditorGUILayout.ObjectField("Culling Shader", _cullingShader, typeof(ComputeShader), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
            _enemyMesh = (Mesh)EditorGUILayout.ObjectField("Enemy Mesh", _enemyMesh, typeof(Mesh), false);
            _enemyMaterial = (Material)EditorGUILayout.ObjectField("Enemy Material", _enemyMaterial, typeof(Material), false);
            _projectileMesh = (Mesh)EditorGUILayout.ObjectField("Projectile Mesh", _projectileMesh, typeof(Mesh), false);
            _projectileMaterial = (Material)EditorGUILayout.ObjectField("Projectile Mat", _projectileMaterial, typeof(Material), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
            _deathVFX = (ParticleSystem)EditorGUILayout.ObjectField("Death VFX Prefab", _deathVFX, typeof(ParticleSystem), false);

            EditorGUILayout.Space(4);
            SerializedObject so = new SerializedObject(this);

            EditorGUILayout.Space(12);
            GUI.enabled = ValidateInputs();
            if (GUILayout.Button("BUILD SCENE", GUILayout.Height(40)))
                BuildScene();
            GUI.enabled = true;
        }

        private bool ValidateInputs()
        {
            return _config && _palette && _level && _cullingShader
                && _enemyMesh && _enemyMaterial
                && _projectileMesh && _projectileMaterial;
        }

        private void BuildScene()
        {
            var root = new GameObject("=== JELLY GUNNER ===");
            Undo.RegisterCreatedObjectUndo(root, "Build JellyGunner Scene");

            var rendererGO = CreateChild(root, "GPU Renderer");
            var renderer = rendererGO.AddComponent<JellyInstanceRenderer>();
            SetPrivateField(renderer, "_cullingShader", _cullingShader);
            SetPrivateField(renderer, "_config", _config);

            var gridGO = CreateChild(root, "Enemy Grid");
            var grid = gridGO.AddComponent<EnemyGridManager>();
            SetPrivateField(grid, "_config", _config);
            SetPrivateField(grid, "_palette", _palette);
            SetPrivateField(grid, "_renderer", renderer);

            var trayAnchor = CreateChild(root, "Tray Anchor");
            trayAnchor.transform.position = new Vector3(0f, 0.5f, 0f);
            var tray = trayAnchor.AddComponent<TraySystem>();
            SetPrivateField(tray, "_config", _config);
            SetPrivateField(tray, "_enemyGrid", grid);
            SetPrivateField(tray, "_trayAnchor", trayAnchor.transform);

            var factoryGO = CreateChild(root, "Blaster Factory");
            var factory = factoryGO.AddComponent<BlasterFactory>();
            SetPrivateField(factory, "_palette", _palette);
            SetPrivateField(factory, "_enemyGrid", grid);

            var supplyAnchor = CreateChild(root, "Supply Anchor");
            supplyAnchor.transform.position = new Vector3(0f, -3f, 0f);
            var supply = supplyAnchor.AddComponent<SupplyLineManager>();
            SetPrivateField(supply, "_config", _config);
            SetPrivateField(supply, "_tray", tray);
            SetPrivateField(supply, "_factory", factory);
            SetPrivateField(supply, "_supplyAnchor", supplyAnchor.transform);

            var projGO = CreateChild(root, "Projectile Manager");
            var projectiles = projGO.AddComponent<ProjectileManager>();
            SetPrivateField(projectiles, "_config", _config);
            SetPrivateField(projectiles, "_palette", _palette);
            SetPrivateField(projectiles, "_renderer", renderer);

            var hammerGO = CreateChild(root, "Hammer PowerUp");
            var hammer = hammerGO.AddComponent<HammerPowerUp>();
            SetPrivateField(hammer, "_config", _config);
            SetPrivateField(hammer, "_enemyGrid", grid);
            SetPrivateField(hammer, "_mainCamera", Camera.main);

            var inputGO = CreateChild(root, "Input Handler");
            var input = inputGO.AddComponent<InputHandler>();
            SetPrivateField(input, "_mainCamera", Camera.main);
            SetPrivateField(input, "_supply", supply);
            SetPrivateField(input, "_hammer", hammer);

            var audioGO = CreateChild(root, "Audio Handler");
            audioGO.AddComponent<AudioHandler>();

            if (_deathVFX)
            {
                var vfxGO = CreateChild(root, "VFX Handler");
                var vfx = vfxGO.AddComponent<VFXHandler>();
                SetPrivateField(vfx, "_palette", _palette);
                SetPrivateField(vfx, "_deathVFXPrefab", _deathVFX);
            }

            if (Camera.main)
                Camera.main.gameObject.AddComponent<GameCameraController>();

            var managerGO = CreateChild(root, "Game Manager");
            var manager = managerGO.AddComponent<GameManager>();
            SetPrivateField(manager, "_config", _config);
            SetPrivateField(manager, "_currentLevel", _level);
            SetPrivateField(manager, "_renderer", renderer);
            SetPrivateField(manager, "_enemyGrid", grid);
            SetPrivateField(manager, "_tray", tray);
            SetPrivateField(manager, "_supply", supply);
            SetPrivateField(manager, "_blasterFactory", factory);
            SetPrivateField(manager, "_hammer", hammer);
            SetPrivateField(manager, "_projectiles", projectiles);
            SetPrivateField(manager, "_enemyMesh", _enemyMesh);
            SetPrivateField(manager, "_enemyMaterial", _enemyMaterial);
            SetPrivateField(manager, "_projectileMesh", _projectileMesh);
            SetPrivateField(manager, "_projectileMaterial", _projectileMaterial);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log("[JellyGunner] Scene built. Don't forget to set up the UI Canvas manually!");
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            return go;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
                field.SetValue(target, value);
        }
    }
}
#endif
