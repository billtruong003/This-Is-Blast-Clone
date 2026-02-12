using System;
using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Central event hub for the drag-and-drop system.
    /// Subscribe to these events to react to drag operations from anywhere.
    /// </summary>
    public static class DragDropEvents
    {
        // ─── Drag Lifecycle ─────────────────────────────────────────
        /// <summary>Fired when a drag begins. Args: (IDraggable, DragContext)</summary>
        public static event Action<IDraggable, DragContext> OnDragStarted;

        /// <summary>Fired every frame during drag. Args: (IDraggable, DragContext)</summary>
        public static event Action<IDraggable, DragContext> OnDragUpdated;

        /// <summary>Fired when a drag ends (any reason). Args: (IDraggable, DragContext, DropResult)</summary>
        public static event Action<IDraggable, DragContext, DropResult> OnDragEnded;

        /// <summary>Fired when drag is cancelled. Args: (IDraggable, DragContext)</summary>
        public static event Action<IDraggable, DragContext> OnDragCancelled;

        // ─── Drop Zone Interactions ─────────────────────────────────
        /// <summary>Fired when draggable enters a drop zone. Args: (IDraggable, IDropZone, DragContext)</summary>
        public static event Action<IDraggable, IDropZone, DragContext> OnDropZoneEnter;

        /// <summary>Fired when draggable exits a drop zone. Args: (IDraggable, IDropZone, DragContext)</summary>
        public static event Action<IDraggable, IDropZone, DragContext> OnDropZoneExit;

        /// <summary>Fired on successful drop. Args: (IDraggable, IDropZone, DragContext)</summary>
        public static event Action<IDraggable, IDropZone, DragContext> OnDropAccepted;

        /// <summary>Fired when drop is rejected. Args: (IDraggable, IDropZone, DragContext)</summary>
        public static event Action<IDraggable, IDropZone, DragContext> OnDropRejected;

        /// <summary>Fired on swap. Args: (IDraggable swapped, IDraggable existing, IDropZone, DragContext)</summary>
        public static event Action<IDraggable, IDraggable, IDropZone, DragContext> OnSwap;

        // ─── Space Transition ───────────────────────────────────────
        /// <summary>
        /// Fired when a drag crosses space boundaries (e.g., UI → 3D).
        /// Args: (IDraggable, DragSpace from, DragSpace to, DragContext)
        /// </summary>
        public static event Action<IDraggable, DragSpace, DragSpace, DragContext> OnSpaceTransition;

        // ─── Internal Invocation Methods ────────────────────────────
        internal static void RaiseDragStarted(IDraggable d, DragContext c) => OnDragStarted?.Invoke(d, c);
        internal static void RaiseDragUpdated(IDraggable d, DragContext c) => OnDragUpdated?.Invoke(d, c);
        internal static void RaiseDragEnded(IDraggable d, DragContext c, DropResult r) => OnDragEnded?.Invoke(d, c, r);
        internal static void RaiseDragCancelled(IDraggable d, DragContext c) => OnDragCancelled?.Invoke(d, c);
        internal static void RaiseDropZoneEnter(IDraggable d, IDropZone z, DragContext c) => OnDropZoneEnter?.Invoke(d, z, c);
        internal static void RaiseDropZoneExit(IDraggable d, IDropZone z, DragContext c) => OnDropZoneExit?.Invoke(d, z, c);
        internal static void RaiseDropAccepted(IDraggable d, IDropZone z, DragContext c) => OnDropAccepted?.Invoke(d, z, c);
        internal static void RaiseDropRejected(IDraggable d, IDropZone z, DragContext c) => OnDropRejected?.Invoke(d, z, c);
        internal static void RaiseSwap(IDraggable a, IDraggable b, IDropZone z, DragContext c) => OnSwap?.Invoke(a, b, z, c);
        internal static void RaiseSpaceTransition(IDraggable d, DragSpace from, DragSpace to, DragContext c) => OnSpaceTransition?.Invoke(d, from, to, c);

        /// <summary>Remove all listeners. Call on scene unload to prevent leaks.</summary>
        public static void ClearAll()
        {
            OnDragStarted = null;
            OnDragUpdated = null;
            OnDragEnded = null;
            OnDragCancelled = null;
            OnDropZoneEnter = null;
            OnDropZoneExit = null;
            OnDropAccepted = null;
            OnDropRejected = null;
            OnSwap = null;
            OnSpaceTransition = null;
        }
    }
}
