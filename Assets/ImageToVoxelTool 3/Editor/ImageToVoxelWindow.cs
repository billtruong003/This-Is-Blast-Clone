using UnityEngine;
using UnityEditor;

namespace ImageToVoxel
{
    public class ImageToVoxelWindow : EditorWindow
    {
        private Texture2D sourceImage;
        private int outputWidth = 64;
        private int outputHeight = 64;
        private int brightnessRanges = 5;
        private float visualScale = 0.2f;
        private float heightMultiplier = 0.3f;
        private bool showGrid = true;
        private bool linkResolution = true;

        private int selectedAspectPreset;
        private readonly string[] aspectPresetLabels = { "Free", "1:1", "16:9", "4:3", "3:2", "9:16", "3:4" };
        private readonly float[] aspectPresetValues = { 0, 1f, 16f / 9f, 4f / 3f, 3f / 2f, 9f / 16f, 3f / 4f };

        private CropSceneHandler cropHandler;
        private VoxelSceneRenderer voxelRenderer;
        private int[,] processedData;

        private Vector2 scrollPosition;
        private bool showCropSection = true;
        private bool showVisualizationSection = true;
        private bool showDataSection = true;
        private bool showTerrainSection = true;

        private Texture2D previewTexture;

        private float terrainCellSize = 0.2f;
        private float terrainMaxDepth = 5f;
        private bool terrainInvertDepth;
        private GameObject terrainPreviewObject;

        private enum ToolState { Idle, CropMode, Visualizing }
        private ToolState currentState = ToolState.Idle;

        [MenuItem("Tools/Image To Voxel")]
        public static void ShowWindow()
        {
            var window = GetWindow<ImageToVoxelWindow>("Image To Voxel");
            window.minSize = new Vector2(320, 500);
        }

        private void OnEnable()
        {
            cropHandler = new CropSceneHandler();
            voxelRenderer = new VoxelSceneRenderer();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            cropHandler?.Dispose();
            voxelRenderer?.Dispose();
            DestroyPreviewTexture();
            DestroyTerrainPreview();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawImageSection();

            if (sourceImage != null)
            {
                DrawResolutionSection();
                DrawBrightnessSection();
                DrawCropSection();
                DrawProcessSection();
                DrawVisualizationSection();
                DrawTerrainSection();
                DrawDataSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("Image To Voxel", headerStyle);
            EditorGUILayout.Space(4);
            DrawSeparator();
        }

        private void DrawImageSection()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Source Image", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newImage = (Texture2D)EditorGUILayout.ObjectField("Texture", sourceImage, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newImage != sourceImage)
                OnImageChanged(newImage);

            if (sourceImage == null)
            {
                EditorGUILayout.HelpBox("Drag and drop a Texture2D above to begin.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Dimensions", $"{sourceImage.width} x {sourceImage.height} px");

            if (!ImageProcessor.IsReadable(sourceImage))
            {
                EditorGUILayout.HelpBox("Texture is not Read/Write enabled. Click the button below to fix this.", MessageType.Warning);
                if (GUILayout.Button("Enable Read/Write"))
                    EnableReadWrite(sourceImage);
            }

            DrawSeparator();
        }

        private void DrawResolutionSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Output Resolution", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            linkResolution = GUILayout.Toggle(linkResolution, "Link W/H", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newWidth = Mathf.Max(2, EditorGUILayout.IntField("Width", outputWidth));
            if (EditorGUI.EndChangeCheck())
            {
                outputWidth = newWidth;
                if (linkResolution)
                    outputHeight = newWidth;
            }

            EditorGUI.BeginChangeCheck();
            int newHeight = Mathf.Max(2, EditorGUILayout.IntField("Height", outputHeight));
            if (EditorGUI.EndChangeCheck())
            {
                outputHeight = newHeight;
                if (linkResolution)
                    outputWidth = newHeight;
            }
            EditorGUILayout.EndHorizontal();

            if (outputWidth * outputHeight > 65536)
                EditorGUILayout.HelpBox("High resolution may impact editor performance.", MessageType.Warning);

            DrawSeparator();
        }

        private void DrawBrightnessSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Brightness Quantization", EditorStyles.boldLabel);
            brightnessRanges = Mathf.Max(2, EditorGUILayout.IntField("Number of Ranges", brightnessRanges));

            EditorGUILayout.Space(2);
            DrawColorPreview();

            DrawSeparator();
        }

        private void DrawColorPreview()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20));
            rect = EditorGUI.IndentedRect(rect);

            for (int i = 0; i < brightnessRanges; i++)
            {
                float t = brightnessRanges > 1 ? i / (float)(brightnessRanges - 1) : 1f;
                float x = rect.x + (rect.width / brightnessRanges) * i;
                float w = rect.width / brightnessRanges;

                EditorGUI.DrawRect(new Rect(x, rect.y, w, rect.height), Color.Lerp(Color.black, Color.white, t));
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.gray);
        }

        private void DrawCropSection()
        {
            EditorGUILayout.Space(4);
            showCropSection = EditorGUILayout.Foldout(showCropSection, "Crop Settings", true, EditorStyles.foldoutHeader);
            if (!showCropSection) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            selectedAspectPreset = EditorGUILayout.Popup("Aspect Ratio", selectedAspectPreset, aspectPresetLabels);
            if (EditorGUI.EndChangeCheck())
            {
                bool locked = selectedAspectPreset > 0;
                float ratio = locked ? aspectPresetValues[selectedAspectPreset] : 0;
                cropHandler.SetAspectRatio(ratio, locked);
                RepaintSceneViewOnce();
            }

            if (GUILayout.Button("Reset Crop"))
            {
                cropHandler.ResetCrop();
                RepaintSceneViewOnce();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Current Crop Region", EditorStyles.miniLabel);
            var pixelCrop = cropHandler.GetPixelCrop();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("X", pixelCrop.x);
            EditorGUILayout.IntField("Y", pixelCrop.y);
            EditorGUILayout.IntField("Width", pixelCrop.width);
            EditorGUILayout.IntField("Height", pixelCrop.height);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            DrawSeparator();
        }

        private void DrawProcessSection()
        {
            EditorGUILayout.Space(8);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            bool canProcess = sourceImage != null && ImageProcessor.IsReadable(sourceImage);
            EditorGUI.BeginDisabledGroup(!canProcess);
            if (GUILayout.Button("Process Image", GUILayout.Height(36)))
                ExecuteProcessing();
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            EditorGUI.BeginDisabledGroup(processedData == null);
            if (GUILayout.Button("Clear Results", GUILayout.Height(28)))
                ClearResults();
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            if (previewTexture != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Quantized Preview", EditorStyles.boldLabel);
                float previewSize = Mathf.Min(position.width - 40, 256);
                var previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
                previewRect.x = (position.width - previewSize) * 0.5f;
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            }

            DrawSeparator();
        }

        private void DrawVisualizationSection()
        {
            if (processedData == null) return;

            EditorGUILayout.Space(4);
            showVisualizationSection = EditorGUILayout.Foldout(showVisualizationSection, "Visualization", true, EditorStyles.foldoutHeader);
            if (!showVisualizationSection) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            visualScale = EditorGUILayout.Slider("Voxel Scale", visualScale, 0.05f, 2f);
            if (EditorGUI.EndChangeCheck())
            {
                voxelRenderer.VisualScale = visualScale;
                RepaintSceneViewOnce();
            }

            EditorGUI.BeginChangeCheck();
            heightMultiplier = EditorGUILayout.Slider("Height Multiplier", heightMultiplier, 0f, 2f);
            if (EditorGUI.EndChangeCheck())
            {
                voxelRenderer.HeightMultiplier = heightMultiplier;
                RepaintSceneViewOnce();
            }

            EditorGUI.BeginChangeCheck();
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
            if (EditorGUI.EndChangeCheck())
            {
                voxelRenderer.ShowGrid = showGrid;
                RepaintSceneViewOnce();
            }

            if (GUILayout.Button("Focus Scene View on Voxels"))
                FocusSceneViewOnVoxels();

            EditorGUI.indentLevel--;
            DrawSeparator();
        }

        private void DrawTerrainSection()
        {
            if (processedData == null) return;

            EditorGUILayout.Space(4);
            showTerrainSection = EditorGUILayout.Foldout(showTerrainSection, "Terrain Depth Mesh", true, EditorStyles.foldoutHeader);
            if (!showTerrainSection) return;

            EditorGUI.indentLevel++;

            terrainCellSize = EditorGUILayout.Slider("Cell Size", terrainCellSize, 0.05f, 2f);
            terrainMaxDepth = EditorGUILayout.Slider("Max Depth", terrainMaxDepth, 0.5f, 20f);
            terrainInvertDepth = EditorGUILayout.Toggle("Invert Depth", terrainInvertDepth);

            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.6f, 0.4f, 0.9f);
            if (GUILayout.Button("Generate Terrain Preview", GUILayout.Height(30)))
                GenerateTerrainPreview();
            GUI.backgroundColor = Color.white;

            EditorGUI.BeginDisabledGroup(terrainPreviewObject == null);
            if (GUILayout.Button("Save Terrain Mesh"))
                SaveTerrainMesh();
            EditorGUI.EndDisabledGroup();

            if (terrainPreviewObject != null)
            {
                EditorGUILayout.Space(2);
                if (GUILayout.Button("Remove Terrain Preview"))
                    DestroyTerrainPreview();
            }

            EditorGUI.indentLevel--;
            DrawSeparator();
        }

        private void GenerateTerrainPreview()
        {
            DestroyTerrainPreview();
            if (processedData == null) return;

            var mesh = TerrainMeshBuilder.Build(processedData, brightnessRanges, terrainCellSize, terrainMaxDepth, terrainInvertDepth);

            var shader = Shader.Find("Hidden/ImageToVoxel/VoxelVertexColor");
            if (shader == null || !shader.isSupported)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            terrainPreviewObject = TerrainMeshBuilder.CreateTerrainObject(mesh, material);

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                var bounds = mesh.bounds;
                sceneView.LookAt(bounds.center, Quaternion.Euler(45, 0, 0), bounds.size.magnitude * 0.8f);
            }
        }

        private void SaveTerrainMesh()
        {
            if (terrainPreviewObject == null) return;
            var filter = terrainPreviewObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            TerrainMeshBuilder.SaveMeshAsset(filter.sharedMesh);
        }

        private void DestroyTerrainPreview()
        {
            if (terrainPreviewObject == null) return;

            var filter = terrainPreviewObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                DestroyImmediate(filter.sharedMesh);

            var renderer = terrainPreviewObject.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
                DestroyImmediate(renderer.sharedMaterial);

            DestroyImmediate(terrainPreviewObject);
            terrainPreviewObject = null;
        }

        private void DrawDataSection()
        {
            if (processedData == null) return;

            EditorGUILayout.Space(4);
            showDataSection = EditorGUILayout.Foldout(showDataSection, "Data Export", true, EditorStyles.foldoutHeader);
            if (!showDataSection) return;

            EditorGUI.indentLevel++;

            int width = processedData.GetLength(0);
            int height = processedData.GetLength(1);
            EditorGUILayout.LabelField("Data Size", $"{width} x {height}");
            EditorGUILayout.LabelField("Total Voxels", (width * height).ToString("N0"));
            EditorGUILayout.LabelField("Range Levels", brightnessRanges.ToString());

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button("Save as ScriptableObject", GUILayout.Height(30)))
                SaveAsScriptableObject();
            GUI.backgroundColor = Color.white;

            EditorGUI.indentLevel--;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            switch (currentState)
            {
                case ToolState.CropMode:
                    cropHandler.OnSceneGUI(sceneView);
                    if (cropHandler.IsDragging)
                        Repaint();
                    break;

                case ToolState.Visualizing:
                    if (Event.current.type == EventType.Repaint)
                        voxelRenderer.DrawOverlay(sceneView);
                    break;
            }
        }

        private void OnImageChanged(Texture2D newImage)
        {
            ClearResults();
            sourceImage = newImage;

            if (sourceImage != null)
            {
                cropHandler.SetImage(sourceImage);
                currentState = ToolState.CropMode;
                selectedAspectPreset = 0;
            }
            else
            {
                cropHandler.SetImage(null);
                currentState = ToolState.Idle;
            }

            RepaintSceneViewOnce();
        }

        private void ExecuteProcessing()
        {
            var pixelCrop = cropHandler.GetPixelCrop();

            processedData = ImageProcessor.Process(
                sourceImage,
                pixelCrop,
                outputWidth,
                outputHeight,
                brightnessRanges
            );

            GeneratePreviewTexture();

            voxelRenderer.VisualScale = visualScale;
            voxelRenderer.HeightMultiplier = heightMultiplier;
            voxelRenderer.ShowGrid = showGrid;
            voxelRenderer.SetData(processedData, brightnessRanges);
            currentState = ToolState.Visualizing;

            RepaintSceneViewOnce();
            FocusSceneViewOnVoxels();
        }

        private void ClearResults()
        {
            processedData = null;
            voxelRenderer.Clear();
            DestroyPreviewTexture();
            DestroyTerrainPreview();

            currentState = sourceImage != null ? ToolState.CropMode : ToolState.Idle;

            RepaintSceneViewOnce();
        }

        private void GeneratePreviewTexture()
        {
            DestroyPreviewTexture();

            if (processedData == null) return;

            int w = processedData.GetLength(0);
            int h = processedData.GetLength(1);
            previewTexture = new Texture2D(w, h, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = brightnessRanges > 1
                        ? processedData[x, y] / (float)(brightnessRanges - 1)
                        : 1f;
                    pixels[y * w + x] = Color.Lerp(Color.black, Color.white, t);
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
        }

        private void SaveAsScriptableObject()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Voxel Map Data",
                "VoxelMapData",
                "asset",
                "Choose a location to save the voxel data."
            );

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<VoxelMapData>();
            asset.Store(processedData, brightnessRanges);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void FocusSceneViewOnVoxels()
        {
            if (processedData == null) return;

            int w = processedData.GetLength(0);
            int h = processedData.GetLength(1);
            float centerX = w * visualScale * 0.5f;
            float centerZ = h * visualScale * 0.5f;
            float size = Mathf.Max(w, h) * visualScale;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                sceneView.LookAt(new Vector3(centerX, 0, centerZ), Quaternion.Euler(60, 0, 0), size * 0.8f);
        }

        private void EnableReadWrite(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot access texture importer.", "OK");
                return;
            }

            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private void DestroyPreviewTexture()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        private static void RepaintSceneViewOnce()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                sceneView.Repaint();
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
