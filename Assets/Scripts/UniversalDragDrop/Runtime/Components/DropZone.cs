using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace Universal.DragDrop
{
    /// <summary>
    /// Attach to any GameObject to make it a drop target.
    /// Works with UI, 2D, and 3D objects. Supports filtering, snapping, capacity, and swap logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class DropZone : MonoBehaviour, IDropZone
    {
        // ─── Inspector Settings ─────────────────────────────────────
        [Header("Drop Zone Configuration")]
        [Tooltip("Which coordinate space does this drop zone operate in?")]
        [SerializeField] private DragSpace _targetSpace = DragSpace.UI;

        [Tooltip("Channel filter — only draggables with matching channel are accepted")]
        [SerializeField] private string _channel = "default";

        [Tooltip("Whether this drop zone is active")]
        [SerializeField] private bool _isDropEnabled = true;

        [Tooltip("How dropped items snap to this zone")]
        [SerializeField] private SnapMode _snapMode = SnapMode.Center;

        [Tooltip("Grid cell size (used when SnapMode = Grid)")]
        [SerializeField] private Vector2 _gridCellSize = new Vector2(1f, 1f);

        [Tooltip("Grid origin offset")]
        [SerializeField] private Vector2 _gridOffset = Vector2.zero;

        [Header("Capacity")]
        [Tooltip("Maximum number of items this zone can hold. 0 = unlimited")]
        [SerializeField] private int _capacity = 1;

        [Tooltip("Allow swapping when zone is full (replaces existing item)")]
        [SerializeField] private bool _allowSwap = false;

        [Header("Visual Feedback")]
        [Tooltip("Color tint when a compatible item hovers")]
        [SerializeField] private Color _hoverValidColor = new Color(0.3f, 1f, 0.3f, 0.5f);

        [Tooltip("Color tint when an incompatible item hovers")]
        [SerializeField] private Color _hoverInvalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);

        [Tooltip("Scale multiplier when hovered")]
        [SerializeField] private float _hoverScale = 1.05f;

        [Tooltip("Show highlight on hover")]
        [SerializeField] private bool _showHighlight = true;

        // ─── Events ─────────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent<IDraggable, DragContext> OnItemDropped;
        public UnityEvent<IDraggable, DragContext> OnItemHoverEnter;
        public UnityEvent<IDraggable, DragContext> OnItemHoverExit;
        public UnityEvent<IDraggable, IDraggable, DragContext> OnItemSwapped;

        /// <summary>Custom validation callback. Return true to accept, false to reject.</summary>
        public Func<IDraggable, DragContext, bool> CustomValidator { get; set; }

        /// <summary>Custom snap position callback.</summary>
        public Func<IDraggable, DragContext, Vector3> CustomSnapPosition { get; set; }

        // ─── Runtime State ──────────────────────────────────────────
        private readonly List<IDraggable> _containedItems = new List<IDraggable>();
        private Vector3 _originalScale;
        private Color _originalColor;
        private Renderer _renderer;
        private UnityEngine.UI.Image _uiImage;
        private SpriteRenderer _spriteRenderer;
        private bool _isHovered;

        // ─── IDropZone Implementation ───────────────────────────────
        public GameObject GameObject => gameObject;
        public DragSpace TargetSpace => _targetSpace;
        public string Channel => _channel;
        public bool IsDropEnabled { get => _isDropEnabled; set => _isDropEnabled = value; }

        /// <summary>Read-only list of items currently in this zone.</summary>
        public IReadOnlyList<IDraggable> ContainedItems => _containedItems;
        public int CurrentCount => _containedItems.Count;
        public bool IsFull => _capacity > 0 && _containedItems.Count >= _capacity;
        public bool IsEmpty => _containedItems.Count == 0;

        // ─── Initialization ─────────────────────────────────────────

        private void Awake()
        {
            _originalScale = transform.localScale;

            // Auto-detect target space
            if (_targetSpace == DragSpace.UI && GetComponent<RectTransform>() == null)
            {
                _targetSpace = GetComponent<Collider2D>() != null ? DragSpace.World2D : DragSpace.World3D;
            }

            // Cache visual components
            _renderer = GetComponent<Renderer>();
            _uiImage = GetComponent<UnityEngine.UI.Image>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_renderer != null) _originalColor = _renderer.material.color;
            else if (_uiImage != null) _originalColor = _uiImage.color;
            else if (_spriteRenderer != null) _originalColor = _spriteRenderer.color;
        }

        private void OnEnable()
        {
            DragDropManager.Instance.RegisterDropZone(this);
        }

        private void OnDisable()
        {
            if (DragDropManager.Instance != null)
                DragDropManager.Instance.UnregisterDropZone(this);
        }

        // ─── IDropZone Callbacks ────────────────────────────────────

        public bool CanAccept(IDraggable draggable, DragContext context)
        {
            if (!_isDropEnabled) return false;

            // Channel check
            if (!string.IsNullOrEmpty(_channel) && !string.IsNullOrEmpty(draggable.Channel))
            {
                if (_channel != draggable.Channel) return false;
            }

            // Capacity check (allow if swapping is enabled)
            if (IsFull && !_allowSwap) return false;

            // Don't accept if it's already here
            if (_containedItems.Contains(draggable)) return false;

            // Custom validator
            if (CustomValidator != null)
                return CustomValidator(draggable, context);

            return true;
        }

        public void OnHoverEnter(IDraggable draggable, DragContext context)
        {
            _isHovered = true;

            if (_showHighlight)
            {
                bool canAccept = CanAccept(draggable, context);
                Color tint = canAccept ? _hoverValidColor : _hoverInvalidColor;
                ApplyColorTint(tint);

                if (_hoverScale != 1f)
                    transform.localScale = _originalScale * _hoverScale;
            }

            OnItemHoverEnter?.Invoke(draggable, context);
        }

        public void OnHoverUpdate(IDraggable draggable, DragContext context)
        {
            // Override in subclass for continuous hover effects (e.g., grid preview)
        }

        public void OnHoverExit(IDraggable draggable, DragContext context)
        {
            _isHovered = false;

            if (_showHighlight)
            {
                RestoreColor();
                transform.localScale = _originalScale;
            }

            OnItemHoverExit?.Invoke(draggable, context);
        }

        public DropResult OnDrop(IDraggable draggable, DragContext context)
        {
            if (!CanAccept(draggable, context))
                return DropResult.Rejected;

            // Handle swap if full
            if (IsFull && _allowSwap && _containedItems.Count > 0)
            {
                IDraggable existingItem = _containedItems[_containedItems.Count - 1];
                _containedItems.RemoveAt(_containedItems.Count - 1);
                _containedItems.Add(draggable);

                DragDropEvents.RaiseSwap(draggable, existingItem, this, context);
                OnItemSwapped?.Invoke(draggable, existingItem, context);

                // Restore hover visual
                RestoreColor();
                transform.localScale = _originalScale;

                return DropResult.Swapped;
            }

            // Normal drop
            _containedItems.Add(draggable);

            // Restore hover visual
            RestoreColor();
            transform.localScale = _originalScale;

            OnItemDropped?.Invoke(draggable, context);

            return DropResult.Accepted;
        }

        public Vector3 GetSnapPosition(IDraggable draggable, DragContext context)
        {
            switch (_snapMode)
            {
                case SnapMode.Center:
                    return Vector3.zero; // Local center

                case SnapMode.Grid:
                    return GetGridSnapPosition(context.CurrentWorldPosition);

                case SnapMode.Custom:
                    if (CustomSnapPosition != null)
                        return CustomSnapPosition(draggable, context);
                    return Vector3.zero;

                case SnapMode.None:
                default:
                    // Return the current world position relative to this zone
                    if (_targetSpace == DragSpace.UI)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            transform as RectTransform,
                            context.CurrentScreenPosition,
                            context.RaycastCamera,
                            out Vector2 localPos
                        );
                        return localPos;
                    }
                    return context.CurrentWorldPosition;
            }
        }

        // ─── Grid Snapping ──────────────────────────────────────────

        private Vector3 GetGridSnapPosition(Vector3 worldPosition)
        {
            // Convert to local space
            Vector3 local = transform.InverseTransformPoint(worldPosition);

            // Snap to grid
            float x = Mathf.Round((local.x - _gridOffset.x) / _gridCellSize.x) * _gridCellSize.x + _gridOffset.x;
            float y = Mathf.Round((local.y - _gridOffset.y) / _gridCellSize.y) * _gridCellSize.y + _gridOffset.y;

            if (_targetSpace == DragSpace.World3D)
            {
                // For 3D, snap XZ and keep Y
                float z = Mathf.Round((local.z - _gridOffset.y) / _gridCellSize.y) * _gridCellSize.y + _gridOffset.y;
                return new Vector3(x, local.y, z);
            }

            return new Vector3(x, y, local.z);
        }

        /// <summary>
        /// Get the world position of a grid cell at the given indices.
        /// </summary>
        public Vector3 GetGridCellWorldPosition(int col, int row)
        {
            Vector3 local = new Vector3(
                col * _gridCellSize.x + _gridOffset.x,
                row * _gridCellSize.y + _gridOffset.y,
                0
            );
            return transform.TransformPoint(local);
        }

        // ─── Visual Feedback ────────────────────────────────────────

        private void ApplyColorTint(Color tint)
        {
            Color mixed = Color.Lerp(_originalColor, tint, 0.5f);

            if (_uiImage != null) _uiImage.color = mixed;
            else if (_spriteRenderer != null) _spriteRenderer.color = mixed;
            else if (_renderer != null && _renderer.material.HasProperty("_Color"))
                _renderer.material.color = mixed;
        }

        private void RestoreColor()
        {
            if (_uiImage != null) _uiImage.color = _originalColor;
            else if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
            else if (_renderer != null && _renderer.material.HasProperty("_Color"))
                _renderer.material.color = _originalColor;
        }

        // ─── Public API ─────────────────────────────────────────────

        /// <summary>Remove an item from this zone (e.g., when it's dragged out).</summary>
        public bool RemoveItem(IDraggable item)
        {
            return _containedItems.Remove(item);
        }

        /// <summary>Clear all items from this zone.</summary>
        public void ClearItems()
        {
            _containedItems.Clear();
        }

        /// <summary>Check if a specific item is in this zone.</summary>
        public bool ContainsItem(IDraggable item)
        {
            return _containedItems.Contains(item);
        }

        /// <summary>Get the first contained item, or null.</summary>
        public IDraggable GetFirstItem()
        {
            return _containedItems.Count > 0 ? _containedItems[0] : null;
        }

        /// <summary>Set the channel for this drop zone.</summary>
        public DropZone SetChannel(string channel)
        {
            _channel = channel;
            return this;
        }

        /// <summary>Set the capacity.</summary>
        public DropZone SetCapacity(int capacity)
        {
            _capacity = capacity;
            return this;
        }
    }
}
