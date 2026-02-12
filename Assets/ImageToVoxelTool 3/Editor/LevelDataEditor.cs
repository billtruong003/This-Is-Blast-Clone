using UnityEngine;
using UnityEditor;
using ImageToVoxel.Game;

namespace ImageToVoxel
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var levelData = (LevelData)target;

            EditorGUILayout.Space(8);

            if (levelData.MapData != null && levelData.MapData.HasData)
            {
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button("Initialize Colors from Map Data", GUILayout.Height(30)))
                {
                    Undo.RecordObject(target, "Initialize Level Colors");
                    levelData.InitializeFromMapData();
                    EditorUtility.SetDirty(target);
                }
                GUI.backgroundColor = Color.white;

                DrawBlastAnalysis(levelData);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a VoxelMapData with data to enable initialization.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBlastAnalysis(LevelData levelData)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Blast Requirements", EditorStyles.boldLabel);

            var blastCounts = levelData.GetAllBlastCounts();
            int totalBlasts = 0;
            var mapCounts = levelData.MapData.CountPerRange();

            foreach (var kvp in blastCounts)
            {
                int blocks = mapCounts.ContainsKey(kvp.Key) ? mapCounts[kvp.Key] : 0;
                EditorGUILayout.BeginHorizontal();

                var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
                EditorGUI.DrawRect(colorRect, levelData.GetColor(kvp.Key));

                EditorGUILayout.LabelField($"Range {kvp.Key}: {blocks} blocks → {kvp.Value} blasts");
                EditorGUILayout.EndHorizontal();

                totalBlasts += kvp.Value;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Total blasts needed: {totalBlasts}", EditorStyles.boldLabel);
        }
    }
}
