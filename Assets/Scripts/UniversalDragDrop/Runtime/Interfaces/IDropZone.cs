using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Interface for any object that can receive a dropped item.
    /// </summary>
    public interface IDropZone
    {
        /// <summary>The GameObject acting as a drop zone.</summary>
        GameObject GameObject { get; }

        /// <summary>The coordinate space this drop zone operates in.</summary>
        DragSpace TargetSpace { get; }

        /// <summary>Channel filter — only draggables with matching channel can be dropped here.</summary>
        string Channel { get; }

        /// <summary>Whether this drop zone is currently accepting drops.</summary>
        bool IsDropEnabled { get; set; }

        /// <summary>Check if a specific draggable can be dropped here.</summary>
        bool CanAccept(IDraggable draggable, DragContext context);

        /// <summary>Called when a draggable enters hover over this zone.</summary>
        void OnHoverEnter(IDraggable draggable, DragContext context);

        /// <summary>Called every frame while a draggable hovers over this zone.</summary>
        void OnHoverUpdate(IDraggable draggable, DragContext context);

        /// <summary>Called when a draggable exits hover over this zone.</summary>
        void OnHoverExit(IDraggable draggable, DragContext context);

        /// <summary>Called when an item is dropped. Return the drop result.</summary>
        DropResult OnDrop(IDraggable draggable, DragContext context);

        /// <summary>Get the snap position for the dropped item (world or local position).</summary>
        Vector3 GetSnapPosition(IDraggable draggable, DragContext context);
    }
}
