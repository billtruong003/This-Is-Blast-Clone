using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ImageToVoxel.Game;

namespace ImageToVoxel
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        private SerializedProperty mapDataProp;
        private SerializedProperty levelDataProp;
        private SerializedProperty rangePrefabsProp;
        private SerializedProperty cellSizeProp;
        private SerializedProperty blocksPerBlastProp;
        private SerializedProperty centerGridProp;
        private SerializedProperty adjustCountsProp;

        private bool showAnalysis = true;
        private MapGenerator generator;

        private void OnEnable()
        {
            generator = (MapGenerator)target;
            mapDataProp = serializedObject.FindProperty("mapData");
            levelDataProp = serializedObject.FindProperty("levelData");
            rangePrefabsProp = serializedObject.FindProperty("rangePrefabs");
            cellSizeProp = serializedObject.FindProperty("cellSize");
            blocksPerBlastProp = serializedObject.FindProperty("blocksPerBlast");
            centerGridProp = serializedObject.FindProperty("centerGrid");
            adjustCountsProp = serializedObject.FindProperty("adjustCountsToDivisible");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDataSection();
            DrawPrefabSection();
            DrawSettingsSection();
            DrawAnalysisSection();
            DrawActionButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDataSection()
        {
            EditorGUILayout.LabelField("Data Sources", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(mapDataProp);
            if (EditorGUI.EndChangeCheck())
                ResizePrefabArray();

            EditorGUILayout.PropertyField(levelDataProp);

            var mapData = mapDataProp.objectReferenceValue as VoxelMapData;
            if (mapData != null && mapData.HasData)
                EditorGUILayout.LabelField("Grid Size", $"{mapData.Width} x {mapData.Height} ({mapData.TotalRanges} ranges)");

            DrawSeparator();
        }

        private void DrawPrefabSection()
        {
            EditorGUILayout.LabelField("Range Prefabs", EditorStyles.boldLabel);

            var mapData = mapDataProp.objectReferenceValue as VoxelMapData;
            if (mapData == null)
            {
                EditorGUILayout.HelpBox("Assign a VoxelMapData to configure prefabs.", MessageType.Info);
                return;
            }

            int expectedSize = mapData.TotalRanges;
            if (rangePrefabsProp.arraySize != expectedSize)
            {
                EditorGUILayout.HelpBox($"Expected {expectedSize} prefabs for {expectedSize} ranges. Array size: {rangePrefabsProp.arraySize}", MessageType.Warning);
                if (GUILayout.Button($"Resize to {expectedSize}"))
                {
                    rangePrefabsProp.arraySize = expectedSize;
                }
            }

            var levelData = levelDataProp.objectReferenceValue as LevelData;

            for (int i = 0; i < rangePrefabsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                Color rangeColor = levelData != null ? levelData.GetColor(i) : GenerateColor(i, rangePrefabsProp.arraySize);
                var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
                EditorGUI.DrawRect(colorRect, rangeColor);

                EditorGUILayout.PropertyField(rangePrefabsProp.GetArrayElementAtIndex(i), new GUIContent($"Range {i}"));
                EditorGUILayout.EndHorizontal();
            }

            DrawSeparator();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(cellSizeProp);
            EditorGUILayout.PropertyField(blocksPerBlastProp);
            EditorGUILayout.PropertyField(centerGridProp);
            EditorGUILayout.PropertyField(adjustCountsProp, new GUIContent("Adjust Counts (÷ Blast)"));

            DrawSeparator();
        }

        private void DrawAnalysisSection()
        {
            var mapData = mapDataProp.objectReferenceValue as VoxelMapData;
            if (mapData == null || !mapData.HasData) return;

            showAnalysis = EditorGUILayout.Foldout(showAnalysis, "Count Analysis", true, EditorStyles.foldoutHeader);
            if (!showAnalysis) return;

            EditorGUI.indentLevel++;

            var rawCounts = generator.GetRawCounts();
            var adjustedCounts = generator.GetAdjustedCounts();
            int blocksPerBlast = blocksPerBlastProp.intValue;
            var levelData = levelDataProp.objectReferenceValue as LevelData;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Range", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Raw", EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Adjusted", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Blasts", EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Status", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            int totalRaw = 0;
            int totalAdjusted = 0;
            int totalBlasts = 0;

            foreach (var kvp in rawCounts)
            {
                int raw = kvp.Value;
                int adjusted = adjustedCounts.ContainsKey(kvp.Key) ? adjustedCounts[kvp.Key] : raw;
                int blasts = Mathf.CeilToInt((float)adjusted / blocksPerBlast);
                bool isDivisible = adjusted % blocksPerBlast == 0;

                totalRaw += raw;
                totalAdjusted += adjusted;
                totalBlasts += blasts;

                Color rangeColor = levelData != null ? levelData.GetColor(kvp.Key) : GenerateColor(kvp.Key, rawCounts.Count);

                EditorGUILayout.BeginHorizontal();

                var colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
                EditorGUI.DrawRect(colorRect, rangeColor);
                EditorGUILayout.LabelField($"  {kvp.Key}", GUILayout.Width(44));
                EditorGUILayout.LabelField(raw.ToString(), GUILayout.Width(50));
                EditorGUILayout.LabelField(adjusted.ToString(), GUILayout.Width(60));
                EditorGUILayout.LabelField(blasts.ToString(), GUILayout.Width(50));
                EditorGUILayout.LabelField(isDivisible ? "OK" : $"+{adjusted - raw}", isDivisible ? EditorStyles.label : EditorStyles.boldLabel);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Total: {totalRaw} raw → {totalAdjusted} adjusted, {totalBlasts} blasts needed", EditorStyles.miniLabel);

            EditorGUI.indentLevel--;
            DrawSeparator();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(8);

            var mapData = mapDataProp.objectReferenceValue as VoxelMapData;
            bool canGenerate = mapData != null && mapData.HasData && rangePrefabsProp.arraySize > 0;

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("Generate Map", GUILayout.Height(36)))
            {
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Map");
                generator.Generate();
                EditorUtility.SetDirty(generator);
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            EditorGUI.BeginDisabledGroup(!generator.HasGenerated);
            if (GUILayout.Button("Clear Generated", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Map");
                generator.ClearGenerated();
                EditorUtility.SetDirty(generator);
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            bool hasMissing = HasMissingPrefabs();
            if (hasMissing)
                EditorGUILayout.HelpBox("Some range prefabs are not assigned. Blocks for those ranges will be skipped.", MessageType.Warning);
        }

        private void ResizePrefabArray()
        {
            serializedObject.ApplyModifiedProperties();
            var mapData = mapDataProp.objectReferenceValue as VoxelMapData;
            if (mapData == null) return;

            serializedObject.Update();
            rangePrefabsProp.arraySize = mapData.TotalRanges;
            serializedObject.ApplyModifiedProperties();
        }

        private bool HasMissingPrefabs()
        {
            for (int i = 0; i < rangePrefabsProp.arraySize; i++)
                if (rangePrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    return true;
            return false;
        }

        private static Color GenerateColor(int index, int total)
        {
            if (total <= 1) return Color.red;
            return Color.HSVToRGB((float)index / total, 0.75f, 0.9f);
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
