using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Base class for custom drag visual behaviors.
    /// Extend this to create specialized visual effects during drag operations.
    /// Attach alongside a Draggable component.
    /// </summary>
    public abstract class DragVisualBase : MonoBehaviour
    {
        protected Draggable _draggable;

        protected virtual void Awake()
        {
            _draggable = GetComponent<Draggable>();
        }

        protected virtual void OnEnable()
        {
            DragDropEvents.OnDragStarted += HandleDragStarted;
            DragDropEvents.OnDragUpdated += HandleDragUpdated;
            DragDropEvents.OnDragEnded += HandleDragEnded;
            DragDropEvents.OnDropZoneEnter += HandleDropZoneEnter;
            DragDropEvents.OnDropZoneExit += HandleDropZoneExit;
            DragDropEvents.OnSpaceTransition += HandleSpaceTransition;
        }

        protected virtual void OnDisable()
        {
            DragDropEvents.OnDragStarted -= HandleDragStarted;
            DragDropEvents.OnDragUpdated -= HandleDragUpdated;
            DragDropEvents.OnDragEnded -= HandleDragEnded;
            DragDropEvents.OnDropZoneEnter -= HandleDropZoneEnter;
            DragDropEvents.OnDropZoneExit -= HandleDropZoneExit;
            DragDropEvents.OnSpaceTransition -= HandleSpaceTransition;
        }

        private void HandleDragStarted(IDraggable d, DragContext c)
        {
            if (d == (IDraggable)_draggable) OnDragVisualStart(c);
        }

        private void HandleDragUpdated(IDraggable d, DragContext c)
        {
            if (d == (IDraggable)_draggable) OnDragVisualUpdate(c);
        }

        private void HandleDragEnded(IDraggable d, DragContext c, DropResult r)
        {
            if (d == (IDraggable)_draggable) OnDragVisualEnd(c, r);
        }

        private void HandleDropZoneEnter(IDraggable d, IDropZone z, DragContext c)
        {
            if (d == (IDraggable)_draggable) OnOverDropZone(z, c, true);
        }

        private void HandleDropZoneExit(IDraggable d, IDropZone z, DragContext c)
        {
            if (d == (IDraggable)_draggable) OnOverDropZone(z, c, false);
        }

        private void HandleSpaceTransition(IDraggable d, DragSpace from, DragSpace to, DragContext c)
        {
            if (d == (IDraggable)_draggable) OnCrossSpaceTransition(from, to, c);
        }

        /// <summary>Called when this object starts being dragged.</summary>
        protected abstract void OnDragVisualStart(DragContext context);

        /// <summary>Called every frame during drag.</summary>
        protected abstract void OnDragVisualUpdate(DragContext context);

        /// <summary>Called when drag ends.</summary>
        protected abstract void OnDragVisualEnd(DragContext context, DropResult result);

        /// <summary>Called when entering/exiting a drop zone.</summary>
        protected virtual void OnOverDropZone(IDropZone zone, DragContext context, bool entering) { }

        /// <summary>Called when drag crosses space boundaries (UI→3D, etc.).</summary>
        protected virtual void OnCrossSpaceTransition(DragSpace from, DragSpace to, DragContext context) { }
    }
}
