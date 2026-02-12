#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JellyGunner.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var level = (LevelData)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("BALANCE REPORT", EditorStyles.boldLabel);

            if (level.waves == null || level.waves.Length == 0)
            {
                EditorGUILayout.HelpBox("No waves defined.", MessageType.Warning);
                return;
            }

            for (int w = 0; w < level.waves.Length; w++)
            {
                var wave = level.waves[w];
                int waveHP = 0;
                int waveAmmo = 0;
                int[] colorHP = new int[4];
                int[] colorAmmo = new int[4];

                if (wave.enemies != null)
                {
                    foreach (var e in wave.enemies)
                    {
                        int hp = e.HP;
                        waveHP += hp;
                        colorHP[(int)e.color] += hp;
                    }
                }

                if (wave.supply != null)
                {
                    foreach (var s in wave.supply)
                    {
                        int ammo = s.Ammo;
                        waveAmmo += ammo;
                        colorAmmo[(int)s.color] += ammo;
                    }
                }

                bool balanced = waveHP == waveAmmo;
                var boxType = balanced ? MessageType.Info : MessageType.Error;
                string status = balanced ? "BALANCED" : "UNBALANCED";

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Wave {w}: {status} (HP: {waveHP} / Ammo: {waveAmmo})");

                string[] colorNames = { "Red", "Blue", "Green", "Yellow" };
                for (int c = 0; c < 4; c++)
                {
                    if (colorHP[c] == 0 && colorAmmo[c] == 0) continue;

                    bool colorOk = colorHP[c] <= colorAmmo[c];
                    var prev = GUI.color;
                    GUI.color = colorOk ? Color.white : new Color(1f, 0.6f, 0.6f);
                    EditorGUILayout.LabelField($"  {colorNames[c]}: HP={colorHP[c]} / Ammo={colorAmmo[c]}");
                    GUI.color = prev;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Open Level Editor"))
                LevelEditorWindow.Open();
        }
    }
}
#endif
