using UnityEngine;
using System.Collections.Generic;

namespace Universal.DragDrop
{
    /// <summary>
    /// Contains all contextual information about an ongoing drag operation.
    /// Passed to all drag/drop callbacks so they have full context.
    /// </summary>
    public class DragContext
    {
        // ─── Identity ───────────────────────────────────────────────
        /// <summary>Unique ID for this drag session.</summary>
        public int SessionId { get; set; }

        /// <summary>The draggable being dragged.</summary>
        public IDraggable Draggable { get; set; }

        /// <summary>The drop zone currently being hovered (null if none).</summary>
        public IDropZone HoveredDropZone { get; set; }

        /// <summary>The drop zone that ultimately received the drop (null until dropped).</summary>
        public IDropZone FinalDropZone { get; set; }

        // ─── Positions ──────────────────────────────────────────────
        /// <summary>Screen position where drag started.</summary>
        public Vector2 StartScreenPosition { get; set; }

        /// <summary>Current screen position of the pointer.</summary>
        public Vector2 CurrentScreenPosition { get; set; }

        /// <summary>World position where drag started (for 2D/3D sources).</summary>
        public Vector3 StartWorldPosition { get; set; }

        /// <summary>Current world position under the pointer (converted based on target space).</summary>
        public Vector3 CurrentWorldPosition { get; set; }

        /// <summary>The original position of the draggable (for return-on-cancel).</summary>
        public Vector3 OriginalPosition { get; set; }

        /// <summary>Original parent transform (for UI reparenting).</summary>
        public Transform OriginalParent { get; set; }

        /// <summary>Original sibling index (for UI ordering).</summary>
        public int OriginalSiblingIndex { get; set; }

        // ─── Raycast Info ───────────────────────────────────────────
        /// <summary>The camera used for this drag session's raycasting.</summary>
        public Camera RaycastCamera { get; set; }

        /// <summary>3D raycast hit info (valid when targeting 3D space).</summary>
        public RaycastHit Hit3D { get; set; }

        /// <summary>2D raycast hit info (valid when targeting 2D space).</summary>
        public RaycastHit2D Hit2D { get; set; }

        /// <summary>The plane used for 3D positioning when no collider is hit.</summary>
        public Plane DragPlane { get; set; }

        // ─── State ──────────────────────────────────────────────────
        /// <summary>Current drag state.</summary>
        public DragState State { get; set; }

        /// <summary>Screen-space delta from last frame.</summary>
        public Vector2 ScreenDelta { get; set; }

        /// <summary>Total drag distance in screen space.</summary>
        public float TotalScreenDistance { get; set; }

        /// <summary>Duration of the drag in seconds.</summary>
        public float DragDuration { get; set; }

        /// <summary>Whether the pointer is currently over any valid drop zone.</summary>
        public bool IsOverDropZone => HoveredDropZone != null;

        // ─── Custom Data ────────────────────────────────────────────
        /// <summary>Custom key-value store for passing data between drag callbacks.</summary>
        public Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();

        /// <summary>Convenience getter/setter for custom data.</summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            CustomData[key] = value;
        }

        /// <summary>Reset context for reuse from pool.</summary>
        public void Reset()
        {
            SessionId = 0;
            Draggable = null;
            HoveredDropZone = null;
            FinalDropZone = null;
            StartScreenPosition = Vector2.zero;
            CurrentScreenPosition = Vector2.zero;
            StartWorldPosition = Vector3.zero;
            CurrentWorldPosition = Vector3.zero;
            OriginalPosition = Vector3.zero;
            OriginalParent = null;
            OriginalSiblingIndex = 0;
            RaycastCamera = null;
            Hit3D = default;
            Hit2D = default;
            DragPlane = default;
            State = DragState.None;
            ScreenDelta = Vector2.zero;
            TotalScreenDistance = 0f;
            DragDuration = 0f;
            CustomData.Clear();
        }
    }
}
