using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Project-wide configuration for the drag-and-drop system.
    /// Create via Assets > Create > Universal DragDrop > Config.
    /// </summary>
    [CreateAssetMenu(fileName = "DragDropConfig", menuName = "Universal DragDrop/Config", order = 1)]
    public class DragDropConfig : ScriptableObject
    {
        [Header("Input")]
        [Tooltip("Input detection method")]
        public InputSource InputSource = InputSource.Auto;

        [Tooltip("Minimum pixel distance before drag starts")]
        public float DragThreshold = 10f;

        [Tooltip("Long press duration to start drag (0 = immediate)")]
        public float LongPressDuration = 0f;

        [Header("Visual")]
        [Tooltip("Default ghost alpha")]
        [Range(0f, 1f)]
        public float GhostAlpha = 0.6f;

        [Tooltip("Default drag scale")]
        public float DragScale = 1.05f;

        [Tooltip("Duration of return animation")]
        public float ReturnDuration = 0.3f;

        [Tooltip("Return animation curve")]
        public AnimationCurve ReturnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("3D Settings")]
        [Tooltip("Default drag plane height")]
        public float DefaultDragPlaneHeight = 0f;

        [Tooltip("Layer mask for 3D raycasts")]
        public LayerMask Raycast3DLayers = -1;

        [Tooltip("Layer mask for 2D raycasts")]
        public LayerMask Raycast2DLayers = -1;

        [Header("Audio")]
        [Tooltip("Sound played when drag starts")]
        public AudioClip DragStartSound;

        [Tooltip("Sound played on successful drop")]
        public AudioClip DropSuccessSound;

        [Tooltip("Sound played on failed drop / cancel")]
        public AudioClip DropFailSound;

        [Tooltip("Sound played when hovering over a valid drop zone")]
        public AudioClip HoverSound;

        // ─── Singleton Access ───────────────────────────────────────
        private static DragDropConfig s_Instance;

        public static DragDropConfig Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<DragDropConfig>("DragDropConfig");
                    if (s_Instance == null)
                    {
                        s_Instance = CreateInstance<DragDropConfig>();
                    }
                }
                return s_Instance;
            }
        }
    }
}
