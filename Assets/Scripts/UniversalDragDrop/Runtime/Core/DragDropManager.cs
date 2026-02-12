using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Universal.DragDrop
{
    /// <summary>
    /// Central manager that orchestrates all drag-and-drop operations.
    /// Handles input detection, space conversion, drop zone detection, and visual management.
    /// Auto-creates itself when first accessed.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DragDropManager : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────
        private static DragDropManager s_Instance;
        public static DragDropManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<DragDropManager>();
                    if (s_Instance == null)
                    {
                        var go = new GameObject("[DragDropManager]");
                        s_Instance = go.AddComponent<DragDropManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        // ─── Settings ──────────────────────────────────────────────
        [Header("General Settings")]
        [Tooltip("Minimum drag distance in pixels before a drag starts")]
        [SerializeField] private float _dragThreshold = 10f;

        [Tooltip("Camera used for raycasting. Leave null to use Camera.main")]
        [SerializeField] private Camera _raycastCamera;

        [Tooltip("Layer mask for 3D raycasts")]
        [SerializeField] private LayerMask _raycast3DLayers = -1;

        [Tooltip("Layer mask for 2D raycasts")]
        [SerializeField] private LayerMask _raycast2DLayers = -1;

        [Tooltip("Default drag plane height for 3D drags")]
        [SerializeField] private float _defaultDragPlaneHeight = 0f;

        [Tooltip("Input source")]
        [SerializeField] private InputSource _inputSource = InputSource.Auto;

        [Header("Return Animation")]
        [Tooltip("Duration of the return animation when a drag is cancelled")]
        [SerializeField] private float _returnDuration = 0.3f;

        [Tooltip("Curve for return animation")]
        [SerializeField] private AnimationCurve _returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ─── Runtime State ──────────────────────────────────────────
        private int _nextSessionId = 1;
        private DragContext _activeContext;
        private IDraggable _activeDraggable;
        private IDropZone _previousHoveredZone;
        private DragSpace _previousDetectedSpace = DragSpace.UI;
        private bool _isDragging;
        private bool _dragStartPending;
        private Vector2 _pointerDownPos;
        private readonly List<DragVisualBase> _activeVisuals = new List<DragVisualBase>();

        // ─── Registered Drop Zones (for manual lookup) ──────────────
        private readonly HashSet<IDropZone> _registeredDropZones = new HashSet<IDropZone>();

        // ─── Properties ─────────────────────────────────────────────
        public bool IsDragging => _isDragging;
        public DragContext ActiveContext => _activeContext;
        public IDraggable ActiveDraggable => _activeDraggable;
        public float DragThreshold { get => _dragThreshold; set => _dragThreshold = value; }
        public float ReturnDuration { get => _returnDuration; set => _returnDuration = value; }

        public Camera RaycastCamera
        {
            get => _raycastCamera != null ? _raycastCamera : Camera.main;
            set => _raycastCamera = value;
        }

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
                DragDropEvents.ClearAll();
            }
        }

        private void Update()
        {
            if (_isDragging)
            {
                UpdateDrag();
            }
        }

        // ─── Registration ───────────────────────────────────────────

        /// <summary>Register a drop zone so the manager knows about it.</summary>
        public void RegisterDropZone(IDropZone zone)
        {
            _registeredDropZones.Add(zone);
        }

        /// <summary>Unregister a drop zone.</summary>
        public void UnregisterDropZone(IDropZone zone)
        {
            _registeredDropZones.Remove(zone);
        }

        // ─── Drag API ───────────────────────────────────────────────

        /// <summary>
        /// Start a drag operation programmatically.
        /// Called by Draggable components when they detect a drag start.
        /// </summary>
        public bool StartDrag(IDraggable draggable, Vector2 screenPosition)
        {
            if (_isDragging)
            {
                Debug.LogWarning("[DragDrop] Cannot start drag while another drag is active.");
                return false;
            }

            if (draggable == null || !draggable.IsDragEnabled)
                return false;

            // Create context
            _activeContext = new DragContext
            {
                SessionId = _nextSessionId++,
                Draggable = draggable,
                StartScreenPosition = screenPosition,
                CurrentScreenPosition = screenPosition,
                RaycastCamera = RaycastCamera,
                State = DragState.Ready
            };

            // Store original position based on source space
            StoreOriginalPosition(draggable, _activeContext);

            // Setup drag plane
            if (draggable.SourceSpace == DragSpace.World3D)
            {
                _activeContext.StartWorldPosition = draggable.GameObject.transform.position;
                _activeContext.DragPlane = SpaceConverter.CreateHorizontalPlane(
                    draggable.GameObject.transform.position.y > 0.01f
                        ? draggable.GameObject.transform.position.y
                        : _defaultDragPlaneHeight
                );
            }
            else if (draggable.SourceSpace == DragSpace.World2D)
            {
                _activeContext.StartWorldPosition = draggable.GameObject.transform.position;
            }

            // Let the draggable know
            if (!draggable.OnDragBegin(_activeContext))
            {
                _activeContext = null;
                return false;
            }

            _activeContext.State = DragState.Dragging;
            _activeDraggable = draggable;
            _isDragging = true;
            _previousHoveredZone = null;
            _previousDetectedSpace = draggable.SourceSpace;

            DragDropEvents.RaiseDragStarted(draggable, _activeContext);

            return true;
        }

        /// <summary>
        /// Cancel the current drag operation. Item returns to original position.
        /// </summary>
        public void CancelDrag()
        {
            if (!_isDragging || _activeContext == null) return;

            _activeContext.State = DragState.Cancelled;

            // Notify hovered zone
            if (_previousHoveredZone != null)
            {
                _previousHoveredZone.OnHoverExit(_activeDraggable, _activeContext);
                DragDropEvents.RaiseDropZoneExit(_activeDraggable, _previousHoveredZone, _activeContext);
            }

            DragDropEvents.RaiseDragCancelled(_activeDraggable, _activeContext);
            _activeDraggable.OnDragEnd(_activeContext, DropResult.Cancelled);
            DragDropEvents.RaiseDragEnded(_activeDraggable, _activeContext, DropResult.Cancelled);

            CleanupDrag();
        }

        /// <summary>
        /// End the current drag at the given screen position, attempting to drop.
        /// Called by Draggable components when pointer is released.
        /// </summary>
        public void EndDrag(Vector2 screenPosition)
        {
            if (!_isDragging || _activeContext == null) return;

            _activeContext.CurrentScreenPosition = screenPosition;
            UpdateWorldPosition();

            DropResult result = DropResult.Cancelled;

            // Find drop zone under pointer
            IDropZone dropZone = SpaceConverter.FindDropZoneAtScreenPosition(
                screenPosition, RaycastCamera, _activeContext, _activeDraggable.Channel
            );

            if (dropZone != null && dropZone.CanAccept(_activeDraggable, _activeContext))
            {
                _activeContext.FinalDropZone = dropZone;
                result = dropZone.OnDrop(_activeDraggable, _activeContext);

                switch (result)
                {
                    case DropResult.Accepted:
                        _activeContext.State = DragState.Dropped;
                        DragDropEvents.RaiseDropAccepted(_activeDraggable, dropZone, _activeContext);
                        break;
                    case DropResult.Rejected:
                        DragDropEvents.RaiseDropRejected(_activeDraggable, dropZone, _activeContext);
                        break;
                    case DropResult.Swapped:
                        _activeContext.State = DragState.Dropped;
                        // Swap event should be raised by the DropZone implementation
                        break;
                }
            }
            else
            {
                result = DropResult.Cancelled;
                _activeContext.State = DragState.Cancelled;
            }

            // Notify previous hovered zone
            if (_previousHoveredZone != null && _previousHoveredZone != dropZone)
            {
                _previousHoveredZone.OnHoverExit(_activeDraggable, _activeContext);
                DragDropEvents.RaiseDropZoneExit(_activeDraggable, _previousHoveredZone, _activeContext);
            }

            _activeDraggable.OnDragEnd(_activeContext, result);
            DragDropEvents.RaiseDragEnded(_activeDraggable, _activeContext, result);

            CleanupDrag();
        }

        // ─── Update Loop ────────────────────────────────────────────

        private void UpdateDrag()
        {
            if (_activeContext == null || _activeDraggable == null)
            {
                CleanupDrag();
                return;
            }

            // Update screen position
            Vector2 prevScreen = _activeContext.CurrentScreenPosition;
            _activeContext.CurrentScreenPosition = GetPointerPosition();
            _activeContext.ScreenDelta = _activeContext.CurrentScreenPosition - prevScreen;
            _activeContext.TotalScreenDistance += _activeContext.ScreenDelta.magnitude;
            _activeContext.DragDuration += Time.deltaTime;

            // Update world position
            UpdateWorldPosition();

            // Detect drop zones
            IDropZone hoveredZone = SpaceConverter.FindDropZoneAtScreenPosition(
                _activeContext.CurrentScreenPosition, RaycastCamera, _activeContext, _activeDraggable.Channel
            );

            // Handle hover transitions
            if (hoveredZone != _previousHoveredZone)
            {
                if (_previousHoveredZone != null)
                {
                    _previousHoveredZone.OnHoverExit(_activeDraggable, _activeContext);
                    DragDropEvents.RaiseDropZoneExit(_activeDraggable, _previousHoveredZone, _activeContext);
                    _activeContext.State = DragState.Dragging;
                }

                if (hoveredZone != null)
                {
                    hoveredZone.OnHoverEnter(_activeDraggable, _activeContext);
                    DragDropEvents.RaiseDropZoneEnter(_activeDraggable, hoveredZone, _activeContext);
                    _activeContext.State = DragState.OverDropZone;

                    // Detect space transition
                    if (hoveredZone.TargetSpace != _previousDetectedSpace)
                    {
                        DragDropEvents.RaiseSpaceTransition(
                            _activeDraggable, _previousDetectedSpace, hoveredZone.TargetSpace, _activeContext
                        );
                        _previousDetectedSpace = hoveredZone.TargetSpace;
                    }
                }

                _previousHoveredZone = hoveredZone;
            }
            else if (hoveredZone != null)
            {
                hoveredZone.OnHoverUpdate(_activeDraggable, _activeContext);
            }

            _activeContext.HoveredDropZone = hoveredZone;

            // Notify draggable
            _activeDraggable.OnDragUpdate(_activeContext);
            DragDropEvents.RaiseDragUpdated(_activeDraggable, _activeContext);

            // Check for cancel input
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelDrag();
            }
        }

        private void UpdateWorldPosition()
        {
            // Determine which target space to use for position calculation
            DragSpace targetSpace = _previousHoveredZone?.TargetSpace ?? _activeDraggable.SourceSpace;

            _activeContext.CurrentWorldPosition = SpaceConverter.ScreenToWorld(
                _activeContext.CurrentScreenPosition, targetSpace, RaycastCamera, _activeContext
            );
        }

        // ─── Helpers ────────────────────────────────────────────────

        private void StoreOriginalPosition(IDraggable draggable, DragContext context)
        {
            var transform = draggable.GameObject.transform;
            context.OriginalPosition = transform.localPosition;

            if (transform is RectTransform)
            {
                context.OriginalParent = transform.parent;
                context.OriginalSiblingIndex = transform.GetSiblingIndex();
            }
        }

        private Vector2 GetPointerPosition()
        {
            // Touch support
            if ((_inputSource == InputSource.Auto || _inputSource == InputSource.Touch) && Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }

            // Mouse fallback
            return Input.mousePosition;
        }

        private void CleanupDrag()
        {
            _activeDraggable = null;
            _activeContext = null;
            _previousHoveredZone = null;
            _isDragging = false;
        }

        // ─── Utility Methods ────────────────────────────────────────

        /// <summary>
        /// Set a custom drag plane for the current drag operation.
        /// Useful for constraining 3D drags to a specific plane.
        /// </summary>
        public void SetDragPlane(Plane plane)
        {
            if (_activeContext != null)
                _activeContext.DragPlane = plane;
        }

        /// <summary>
        /// Force-update the raycast camera for the current session.
        /// </summary>
        public void SetSessionCamera(Camera camera)
        {
            if (_activeContext != null)
                _activeContext.RaycastCamera = camera;
        }
    }
}
