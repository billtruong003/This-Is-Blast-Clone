namespace Universal.DragDrop
{
    /// <summary>
    /// Defines the coordinate space a draggable or drop zone operates in.
    /// </summary>
    public enum DragSpace
    {
        /// <summary>UI space (RectTransform / Canvas)</summary>
        UI,
        /// <summary>2D world space (uses Camera.main or specified camera)</summary>
        World2D,
        /// <summary>3D world space (raycast against colliders/plane)</summary>
        World3D
    }

    /// <summary>
    /// Current state of a drag operation.
    /// </summary>
    public enum DragState
    {
        None,
        Ready,
        Dragging,
        OverDropZone,
        Dropped,
        Cancelled
    }

    /// <summary>
    /// How the drag visual follows the cursor.
    /// </summary>
    public enum DragVisualMode
    {
        /// <summary>Move the original object</summary>
        MoveOriginal,
        /// <summary>Create a ghost/clone that follows cursor</summary>
        Ghost,
        /// <summary>Show a custom preview prefab</summary>
        CustomPreview,
        /// <summary>No visual — only logic (useful for data-only drags)</summary>
        None
    }

    /// <summary>
    /// Determines how a dropped item snaps to the drop zone.
    /// </summary>
    public enum SnapMode
    {
        /// <summary>No snapping, stays where dropped</summary>
        None,
        /// <summary>Snap to drop zone center</summary>
        Center,
        /// <summary>Snap to nearest grid cell</summary>
        Grid,
        /// <summary>Custom snap logic via callback</summary>
        Custom
    }

    /// <summary>
    /// The result of a drop attempt.
    /// </summary>
    public enum DropResult
    {
        /// <summary>Drop was accepted</summary>
        Accepted,
        /// <summary>Drop was rejected, item returns to origin</summary>
        Rejected,
        /// <summary>Drop triggered a swap with existing item</summary>
        Swapped,
        /// <summary>Drop was cancelled by user</summary>
        Cancelled
    }

    /// <summary>
    /// Input source for drag detection.
    /// </summary>
    public enum InputSource
    {
        /// <summary>Auto-detect (mouse on desktop, touch on mobile)</summary>
        Auto,
        Mouse,
        Touch,
        /// <summary>New Input System pointer</summary>
        InputSystem
    }
}
