using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Interface for any object that can be dragged.
    /// Implement this to create custom draggable behaviors.
    /// </summary>
    public interface IDraggable
    {
        /// <summary>The GameObject being dragged.</summary>
        GameObject GameObject { get; }

        /// <summary>The coordinate space this draggable operates in.</summary>
        DragSpace SourceSpace { get; }

        /// <summary>Current drag state.</summary>
        DragState State { get; }

        /// <summary>Unique channel/tag for filtering which drop zones accept this draggable.</summary>
        string Channel { get; }

        /// <summary>Arbitrary data payload carried by this draggable (item data, unit info, etc.).</summary>
        object DragData { get; set; }

        /// <summary>Whether this draggable is currently enabled.</summary>
        bool IsDragEnabled { get; set; }

        /// <summary>Called when drag begins. Return false to cancel.</summary>
        bool OnDragBegin(DragContext context);

        /// <summary>Called every frame during drag.</summary>
        void OnDragUpdate(DragContext context);

        /// <summary>Called when drag ends (drop or cancel).</summary>
        void OnDragEnd(DragContext context, DropResult result);
    }
}
