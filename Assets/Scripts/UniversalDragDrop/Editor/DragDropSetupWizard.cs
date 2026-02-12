using UnityEngine;
using UnityEditor;

namespace Universal.DragDrop.Editor
{
    /// <summary>
    /// Editor window for quick drag-drop component setup.
    /// Access via menu: Tools > Universal DragDrop > Setup Wizard
    /// </summary>
    public class DragDropSetupWizard : EditorWindow
    {
        private enum SetupMode
        {
            UIInventory,
            UIto3DDeploy,
            World3DMovement,
            UIto2DTowerDefense,
            Custom
        }

        private SetupMode _mode = SetupMode.UIInventory;
        private string _channel = "default";

        [MenuItem("Tools/Universal DragDrop/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<DragDropSetupWizard>("DragDrop Setup");
        }

        [MenuItem("Tools/Universal DragDrop/Add Draggable %#d")]
        public static void AddDraggable()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<Draggable>(Selection.activeGameObject);
            }
        }

        [MenuItem("Tools/Universal DragDrop/Add Drop Zone %#z")]
        public static void AddDropZone()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<DropZone>(Selection.activeGameObject);
            }
        }

        [MenuItem("Tools/Universal DragDrop/Add Grid Drop Zone")]
        public static void AddGridDropZone()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<GridDropZone>(Selection.activeGameObject);
            }
        }

        [MenuItem("Tools/Universal DragDrop/Add Inventory Slot")]
        public static void AddInventorySlot()
        {
            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.GetComponent<DropZone>() == null)
                    Undo.AddComponent<DropZone>(Selection.activeGameObject);
                Undo.AddComponent<InventorySlot>(Selection.activeGameObject);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Universal Drag & Drop Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _mode = (SetupMode)EditorGUILayout.EnumPopup("Setup Mode", _mode);
            _channel = EditorGUILayout.TextField("Channel", _channel);

            EditorGUILayout.Space();

            int selectedCount = Selection.gameObjects.Length;
            EditorGUILayout.HelpBox(
                selectedCount > 0
                    ? $"{selectedCount} GameObject(s) selected"
                    : "Select GameObjects in the scene to setup",
                selectedCount > 0 ? MessageType.Info : MessageType.Warning
            );

            EditorGUILayout.Space();

            switch (_mode)
            {
                case SetupMode.UIInventory:
                    DrawUIInventorySetup();
                    break;
                case SetupMode.UIto3DDeploy:
                    DrawUIto3DSetup();
                    break;
                case SetupMode.World3DMovement:
                    Draw3DMovementSetup();
                    break;
                case SetupMode.UIto2DTowerDefense:
                    DrawUIto2DSetup();
                    break;
                case SetupMode.Custom:
                    DrawCustomSetup();
                    break;
            }
        }

        private void DrawUIInventorySetup()
        {
            EditorGUILayout.LabelField("UI ↔ UI Inventory Setup", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Adds Draggable + DropZone + InventorySlot to selected UI elements.\n" +
                "Each slot can hold one item with drag-to-swap support.",
                MessageType.None
            );

            if (GUILayout.Button("Setup as Inventory Slots") && Selection.gameObjects.Length > 0)
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup Inventory Slot");
                    SetupComponent<Draggable>(go, d => {
                        SetPrivateField(d, "_sourceSpace", DragSpace.UI);
                        SetPrivateField(d, "_channel", _channel);
                        SetPrivateField(d, "_visualMode", DragVisualMode.MoveOriginal);
                    });
                    SetupComponent<DropZone>(go, d => {
                        SetPrivateField(d, "_targetSpace", DragSpace.UI);
                        SetPrivateField(d, "_channel", _channel);
                        SetPrivateField(d, "_capacity", 1);
                        SetPrivateField(d, "_allowSwap", true);
                        SetPrivateField(d, "_snapMode", SnapMode.Center);
                    });
                    SetupComponent<InventorySlot>(go);
                }
            }
        }

        private void DrawUIto3DSetup()
        {
            EditorGUILayout.LabelField("UI → 3D Deploy Setup", EditorStyles.miniLabel);

            if (GUILayout.Button("Setup Selected as UI Draggables"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup UI Draggable");
                    SetupComponent<Draggable>(go, d => {
                        SetPrivateField(d, "_sourceSpace", DragSpace.UI);
                        SetPrivateField(d, "_channel", _channel);
                        SetPrivateField(d, "_visualMode", DragVisualMode.Ghost);
                    });
                    SetupComponent<WorldPreviewVisual>(go);
                }
            }

            if (GUILayout.Button("Setup Selected as 3D Drop Zones"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup 3D Drop Zone");
                    SetupComponent<DropZone>(go, d => {
                        SetPrivateField(d, "_targetSpace", DragSpace.World3D);
                        SetPrivateField(d, "_channel", _channel);
                    });

                    // Ensure collider exists
                    if (go.GetComponent<Collider>() == null)
                    {
                        Undo.AddComponent<BoxCollider>(go);
                    }
                }
            }
        }

        private void Draw3DMovementSetup()
        {
            EditorGUILayout.LabelField("3D → 3D Movement Setup", EditorStyles.miniLabel);

            if (GUILayout.Button("Setup Selected as 3D Draggables"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup 3D Draggable");
                    SetupComponent<Draggable>(go, d => {
                        SetPrivateField(d, "_sourceSpace", DragSpace.World3D);
                        SetPrivateField(d, "_channel", _channel);
                        SetPrivateField(d, "_visualMode", DragVisualMode.MoveOriginal);
                    });
                }
            }

            if (GUILayout.Button("Setup Selected as 3D Drop Zones"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup 3D Drop Zone");
                    SetupComponent<DropZone>(go, d => {
                        SetPrivateField(d, "_targetSpace", DragSpace.World3D);
                        SetPrivateField(d, "_channel", _channel);
                    });
                }
            }
        }

        private void DrawUIto2DSetup()
        {
            EditorGUILayout.LabelField("UI → 2D Tower Defense Setup", EditorStyles.miniLabel);

            if (GUILayout.Button("Setup Selected as UI Draggables"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup UI Draggable");
                    SetupComponent<Draggable>(go, d => {
                        SetPrivateField(d, "_sourceSpace", DragSpace.UI);
                        SetPrivateField(d, "_channel", _channel);
                        SetPrivateField(d, "_visualMode", DragVisualMode.Ghost);
                    });
                }
            }

            if (GUILayout.Button("Setup Selected as 2D Grid Drop Zones"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Setup 2D Grid");
                    SetupComponent<GridDropZone>(go, d => {
                        SetPrivateField(d, "_targetSpace", DragSpace.World2D);
                        SetPrivateField(d, "_channel", _channel);
                    });

                    if (go.GetComponent<Collider2D>() == null)
                    {
                        Undo.AddComponent<BoxCollider2D>(go);
                    }
                }
            }
        }

        private void DrawCustomSetup()
        {
            EditorGUILayout.LabelField("Custom Setup", EditorStyles.miniLabel);

            if (GUILayout.Button("Add Draggable")) AddDraggable();
            if (GUILayout.Button("Add DropZone")) AddDropZone();
            if (GUILayout.Button("Add GridDropZone")) AddGridDropZone();
            if (GUILayout.Button("Add InventorySlot")) AddInventorySlot();
            if (GUILayout.Button("Add WorldPreviewVisual"))
            {
                foreach (var go in Selection.gameObjects)
                    SetupComponent<WorldPreviewVisual>(go);
            }
        }

        // ─── Helpers ────────────────────────────────────────────────

        private T SetupComponent<T>(GameObject go, System.Action<T> configure = null) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
                comp = Undo.AddComponent<T>(go);
            configure?.Invoke(comp);
            EditorUtility.SetDirty(go);
            return comp;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                // Try serialized field via SerializedObject
                var so = new SerializedObject(target as Component);
                var prop = so.FindProperty(fieldName);
                if (prop != null)
                {
                    switch (value)
                    {
                        case string s: prop.stringValue = s; break;
                        case int i: prop.intValue = i; break;
                        case float f: prop.floatValue = f; break;
                        case bool b: prop.boolValue = b; break;
                        default:
                            if (value is System.Enum e)
                                prop.enumValueIndex = (int)(object)e;
                            break;
                    }
                    so.ApplyModifiedProperties();
                }
            }
        }
    }
}
