using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Universal.DragDrop
{
    /// <summary>
    /// Attach to any GameObject to make it draggable.
    /// Works with UI (RectTransform), 2D (Collider2D), and 3D (Collider) objects.
    /// Handles input detection and delegates to DragDropManager for orchestration.
    /// </summary>
    [DisallowMultipleComponent]
    public class Draggable : MonoBehaviour, IDraggable, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        // ─── Inspector Settings ─────────────────────────────────────
        [Header("Drag Configuration")]
        [Tooltip("Which coordinate space does this object live in?")]
        [SerializeField] private DragSpace _sourceSpace = DragSpace.UI;

        [Tooltip("Channel for filtering compatible drop zones")]
        [SerializeField] private string _channel = "default";

        [Tooltip("How the drag visual behaves")]
        [SerializeField] private DragVisualMode _visualMode = DragVisualMode.MoveOriginal;

        [Tooltip("Custom preview prefab (used when VisualMode = CustomPreview)")]
        [SerializeField] private GameObject _previewPrefab;

        [Tooltip("Minimum distance in pixels before drag starts")]
        [SerializeField] private float _dragThreshold = 5f;

        [Tooltip("Whether this draggable is enabled")]
        [SerializeField] private bool _isDragEnabled = true;

        [Tooltip("Alpha of the ghost visual")]
        [SerializeField, Range(0f, 1f)] private float _ghostAlpha = 0.6f;

        [Tooltip("Return to original position when cancelled (auto-handled for MoveOriginal mode)")]
        [SerializeField] private bool _returnOnCancel = true;

        [Tooltip("Duration of return animation")]
        [SerializeField] private float _returnDuration = 0.25f;

        [Tooltip("Scale multiplier while dragging")]
        [SerializeField] private float _dragScale = 1.05f;

        [Tooltip("Canvas to reparent to during drag (UI only). Leave null for auto-detect.")]
        [SerializeField] private Canvas _dragCanvas;

        [Header("3D/2D Settings")]
        [Tooltip("Offset from the pointer when dragging in world space")]
        [SerializeField] private Vector3 _worldDragOffset = Vector3.zero;

        [Tooltip("Custom drag plane normal (3D only). Zero = auto (horizontal plane)")]
        [SerializeField] private Vector3 _customPlaneNormal = Vector3.zero;

        [Tooltip("Camera for raycasting. Leave null to use manager's camera.")]
        [SerializeField] private Camera _overrideCamera;

        // ─── Events ─────────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent<DragContext> OnDragStartEvent;
        public UnityEvent<DragContext> OnDragUpdateEvent;
        public UnityEvent<DragContext, DropResult> OnDragEndEvent;

        // ─── Runtime State ──────────────────────────────────────────
        private DragContext _currentContext;
        private GameObject _ghostObject;
        private Vector3 _originalScale;
        private bool _isPointerDown;
        private Vector2 _pointerDownPosition;
        private bool _dragInitiated;
        private Canvas _rootCanvas;
        private CanvasGroup _originalCanvasGroup;
        private int _originalSortOrder;

        // ─── IDraggable Implementation ──────────────────────────────
        public GameObject GameObject => gameObject;
        public DragSpace SourceSpace => _sourceSpace;
        public DragState State => _currentContext?.State ?? DragState.None;
        public string Channel => _channel;
        public object DragData { get; set; }
        public bool IsDragEnabled { get => _isDragEnabled; set => _isDragEnabled = value; }
        public DragVisualMode VisualMode { get => _visualMode; set => _visualMode = value; }
        public DragContext CurrentContext => _currentContext;

        // ─── Initialization ─────────────────────────────────────────

        private void Awake()
        {
            _originalScale = transform.localScale;

            // Auto-detect source space
            if (_sourceSpace == DragSpace.UI && GetComponent<RectTransform>() == null)
            {
                _sourceSpace = GetComponent<Collider2D>() != null ? DragSpace.World2D : DragSpace.World3D;
            }

            // Find root canvas for UI dragging
            if (_sourceSpace == DragSpace.UI)
            {
                _rootCanvas = _dragCanvas;
                if (_rootCanvas == null)
                    _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            }
        }

        // ─── UI Input Handlers (EventSystem) ────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isDragEnabled) return;
            _isPointerDown = true;
            _pointerDownPosition = eventData.position;
            _dragInitiated = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;

            if (_dragInitiated && DragDropManager.Instance.IsDragging)
            {
                DragDropManager.Instance.EndDrag(eventData.position);
            }
            _dragInitiated = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isDragEnabled || !_isPointerDown) return;

            if (Vector2.Distance(eventData.position, _pointerDownPosition) < _dragThreshold)
                return;

            _dragInitiated = DragDropManager.Instance.StartDrag(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Handled by DragDropManager.Update()
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragInitiated && DragDropManager.Instance.IsDragging)
            {
                DragDropManager.Instance.EndDrag(eventData.position);
            }
            _dragInitiated = false;
            _isPointerDown = false;
        }

        // ─── 3D/2D Input Handlers (non-UI) ─────────────────────────

        private void OnMouseDown()
        {
            if (_sourceSpace == DragSpace.UI || !_isDragEnabled) return;
            _isPointerDown = true;
            _pointerDownPosition = Input.mousePosition;
            _dragInitiated = false;
        }

        private void OnMouseDrag()
        {
            if (_sourceSpace == DragSpace.UI || !_isDragEnabled || !_isPointerDown) return;

            if (!_dragInitiated)
            {
                if (Vector2.Distance((Vector2)Input.mousePosition, _pointerDownPosition) >= _dragThreshold)
                {
                    _dragInitiated = DragDropManager.Instance.StartDrag(this, Input.mousePosition);
                }
            }
        }

        private void OnMouseUp()
        {
            if (_sourceSpace == DragSpace.UI) return;

            if (_dragInitiated && DragDropManager.Instance.IsDragging)
            {
                DragDropManager.Instance.EndDrag(Input.mousePosition);
            }
            _dragInitiated = false;
            _isPointerDown = false;
        }

        // ─── IDraggable Callbacks ───────────────────────────────────

        public bool OnDragBegin(DragContext context)
        {
            _currentContext = context;

            // Override camera if specified
            if (_overrideCamera != null)
                context.RaycastCamera = _overrideCamera;

            // Custom plane
            if (_customPlaneNormal != Vector3.zero && _sourceSpace == DragSpace.World3D)
            {
                context.DragPlane = new Plane(_customPlaneNormal.normalized, transform.position);
            }

            // Setup visual
            switch (_visualMode)
            {
                case DragVisualMode.MoveOriginal:
                    SetupMoveOriginal(context);
                    break;
                case DragVisualMode.Ghost:
                    CreateGhost(context);
                    break;
                case DragVisualMode.CustomPreview:
                    CreatePreview(context);
                    break;
            }

            // Scale feedback
            if (_dragScale != 1f)
                transform.localScale = _originalScale * _dragScale;

            OnDragStartEvent?.Invoke(context);
            return true;
        }

        public void OnDragUpdate(DragContext context)
        {
            switch (_visualMode)
            {
                case DragVisualMode.MoveOriginal:
                    UpdateMoveOriginal(context);
                    break;
                case DragVisualMode.Ghost:
                    UpdateGhost(context);
                    break;
                case DragVisualMode.CustomPreview:
                    UpdatePreview(context);
                    break;
            }

            OnDragUpdateEvent?.Invoke(context);
        }

        public void OnDragEnd(DragContext context, DropResult result)
        {
            // Restore scale
            transform.localScale = _originalScale;

            switch (result)
            {
                case DropResult.Accepted:
                case DropResult.Swapped:
                    HandleDropAccepted(context);
                    break;
                case DropResult.Rejected:
                case DropResult.Cancelled:
                    HandleDropCancelled(context);
                    break;
            }

            // Cleanup ghost
            CleanupVisual();

            _currentContext = null;
            OnDragEndEvent?.Invoke(context, result);
        }

        // ─── Visual Handling ────────────────────────────────────────

        private void SetupMoveOriginal(DragContext context)
        {
            if (_sourceSpace == DragSpace.UI)
            {
                // Reparent to root canvas so we draw on top
                if (_rootCanvas != null)
                {
                    context.OriginalParent = transform.parent;
                    context.OriginalSiblingIndex = transform.GetSiblingIndex();
                    transform.SetParent(_rootCanvas.transform, true);
                    transform.SetAsLastSibling();
                }

                // Disable raycast on this element so we can detect zones below
                _originalCanvasGroup = GetComponent<CanvasGroup>();
                if (_originalCanvasGroup == null)
                    _originalCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                _originalCanvasGroup.blocksRaycasts = false;
            }
        }

        private void UpdateMoveOriginal(DragContext context)
        {
            switch (_sourceSpace)
            {
                case DragSpace.UI:
                    if (transform is RectTransform rt)
                    {
                        rt.position = context.CurrentScreenPosition;
                    }
                    break;
                case DragSpace.World2D:
                case DragSpace.World3D:
                    transform.position = context.CurrentWorldPosition + _worldDragOffset;
                    break;
            }
        }

        private void CreateGhost(DragContext context)
        {
            _ghostObject = Instantiate(gameObject);
            _ghostObject.name = gameObject.name + " (Ghost)";

            // Remove Draggable from ghost to prevent input conflicts
            var ghostDraggable = _ghostObject.GetComponent<Draggable>();
            if (ghostDraggable != null) Destroy(ghostDraggable);

            // Remove colliders from ghost
            foreach (var col in _ghostObject.GetComponents<Collider>()) Destroy(col);
            foreach (var col2D in _ghostObject.GetComponents<Collider2D>()) Destroy(col2D);

            // Set ghost alpha
            SetAlpha(_ghostObject, _ghostAlpha);

            if (_sourceSpace == DragSpace.UI)
            {
                if (_rootCanvas != null)
                {
                    _ghostObject.transform.SetParent(_rootCanvas.transform, false);
                    _ghostObject.transform.SetAsLastSibling();
                }

                var cg = _ghostObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = _ghostObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.alpha = _ghostAlpha;
            }

            // Fade original slightly
            SetAlpha(gameObject, 0.3f);
        }

        private void UpdateGhost(DragContext context)
        {
            if (_ghostObject == null) return;

            switch (_sourceSpace)
            {
                case DragSpace.UI:
                    _ghostObject.transform.position = context.CurrentScreenPosition;
                    break;
                case DragSpace.World2D:
                case DragSpace.World3D:
                    _ghostObject.transform.position = context.CurrentWorldPosition + _worldDragOffset;
                    break;
            }
        }

        private void CreatePreview(DragContext context)
        {
            if (_previewPrefab == null)
            {
                Debug.LogWarning($"[DragDrop] No preview prefab set on {gameObject.name}, falling back to ghost.");
                _visualMode = DragVisualMode.Ghost;
                CreateGhost(context);
                return;
            }

            _ghostObject = Instantiate(_previewPrefab);
            _ghostObject.name = gameObject.name + " (Preview)";

            if (_sourceSpace == DragSpace.UI && _rootCanvas != null)
            {
                _ghostObject.transform.SetParent(_rootCanvas.transform, false);
                _ghostObject.transform.SetAsLastSibling();

                var cg = _ghostObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = _ghostObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
            }
        }

        private void UpdatePreview(DragContext context)
        {
            UpdateGhost(context); // Same logic
        }

        private void HandleDropAccepted(DragContext context)
        {
            if (_visualMode == DragVisualMode.MoveOriginal && _sourceSpace == DragSpace.UI)
            {
                // Reparent to drop zone if it's UI
                if (context.FinalDropZone?.TargetSpace == DragSpace.UI)
                {
                    var dropTransform = context.FinalDropZone.GameObject.transform;
                    transform.SetParent(dropTransform, false);

                    // Snap to position
                    Vector3 snapPos = context.FinalDropZone.GetSnapPosition(this, context);
                    transform.localPosition = snapPos;
                }

                // Re-enable raycasts
                if (_originalCanvasGroup != null)
                    _originalCanvasGroup.blocksRaycasts = true;
            }
            else if (_visualMode == DragVisualMode.MoveOriginal)
            {
                // Snap to world position
                if (context.FinalDropZone != null)
                {
                    Vector3 snapPos = context.FinalDropZone.GetSnapPosition(this, context);
                    transform.position = snapPos;
                }
            }
        }

        private void HandleDropCancelled(DragContext context)
        {
            if (!_returnOnCancel) return;

            if (_visualMode == DragVisualMode.MoveOriginal)
            {
                if (_sourceSpace == DragSpace.UI)
                {
                    // Reparent back
                    if (context.OriginalParent != null)
                    {
                        transform.SetParent(context.OriginalParent, false);
                        transform.SetSiblingIndex(context.OriginalSiblingIndex);
                    }
                    transform.localPosition = context.OriginalPosition;

                    if (_originalCanvasGroup != null)
                        _originalCanvasGroup.blocksRaycasts = true;
                }
                else
                {
                    // Animate return for 3D/2D
                    StartCoroutine(AnimateReturn(context.OriginalPosition));
                }
            }

            // Restore original alpha
            SetAlpha(gameObject, 1f);
        }

        private System.Collections.IEnumerator AnimateReturn(Vector3 targetPosition)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < _returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _returnDuration);
                t = t * t * (3f - 2f * t); // Smoothstep
                transform.position = Vector3.Lerp(start, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
        }

        private void CleanupVisual()
        {
            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
                _ghostObject = null;
            }

            SetAlpha(gameObject, 1f);
        }

        // ─── Utility ────────────────────────────────────────────────

        private static void SetAlpha(GameObject obj, float alpha)
        {
            // CanvasGroup (UI)
            var canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                return;
            }

            // SpriteRenderer (2D)
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
                return;
            }

            // MeshRenderer (3D)
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.material.HasProperty("_Color"))
            {
                var color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }

        // ─── Public API ─────────────────────────────────────────────

        /// <summary>Set the data payload for this draggable.</summary>
        public Draggable SetData(object data)
        {
            DragData = data;
            return this;
        }

        /// <summary>Set the drag channel.</summary>
        public Draggable SetChannel(string channel)
        {
            _channel = channel;
            return this;
        }

        /// <summary>Set the source space.</summary>
        public Draggable SetSourceSpace(DragSpace space)
        {
            _sourceSpace = space;
            return this;
        }

        /// <summary>Start dragging this object programmatically.</summary>
        public bool StartDragProgrammatic(Vector2 screenPosition)
        {
            return DragDropManager.Instance.StartDrag(this, screenPosition);
        }
    }
}
