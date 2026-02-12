using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Universal.DragDrop
{
    /// <summary>
    /// Pre-built inventory slot component for UI-to-UI drag and drop.
    /// Manages slot state, item display, quantity, and swap behavior.
    /// Attach to a UI element that has both a DropZone and optionally a Draggable.
    /// </summary>
    [RequireComponent(typeof(DropZone))]
    public class InventorySlot : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [Tooltip("Unique slot index or ID")]
        [SerializeField] private int _slotIndex;

        [Tooltip("Image to display the item icon")]
        [SerializeField] private Image _itemIcon;

        [Tooltip("Text to display item quantity")]
        [SerializeField] private TextMeshProUGUI _quantityText;

        [Tooltip("Background image for the slot")]
        [SerializeField] private Image _backgroundImage;

        [Tooltip("Default slot color")]
        [SerializeField] private Color _defaultColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Tooltip("Color when slot is empty and being hovered")]
        [SerializeField] private Color _emptyHoverColor = new Color(0.4f, 0.8f, 0.4f, 0.8f);

        [Tooltip("Color when slot is occupied and being hovered (swap)")]
        [SerializeField] private Color _swapHoverColor = new Color(0.8f, 0.8f, 0.2f, 0.8f);

        [Header("Events")]
        public UnityEvent<InventorySlot, object> OnItemPlaced;
        public UnityEvent<InventorySlot, object> OnItemRemoved;
        public UnityEvent<InventorySlot, InventorySlot> OnItemSwapped;

        // ─── Runtime State ──────────────────────────────────────────
        private DropZone _dropZone;
        private Draggable _draggable;
        private object _itemData;
        private Sprite _currentSprite;
        private int _quantity;

        // ─── Properties ─────────────────────────────────────────────
        public int SlotIndex => _slotIndex;
        public object ItemData => _itemData;
        public bool HasItem => _itemData != null;
        public Sprite CurrentSprite => _currentSprite;
        public int Quantity => _quantity;
        public DropZone DropZone => _dropZone;

        // ─── Initialization ─────────────────────────────────────────

        private void Awake()
        {
            _dropZone = GetComponent<DropZone>();
            _draggable = GetComponent<Draggable>();

            // Setup drop zone events
            _dropZone.OnItemDropped.AddListener(HandleItemDropped);
            _dropZone.OnItemHoverEnter.AddListener(HandleHoverEnter);
            _dropZone.OnItemHoverExit.AddListener(HandleHoverExit);
            _dropZone.OnItemSwapped.AddListener(HandleItemSwapped);

            // Setup draggable events
            if (_draggable != null)
            {
                _draggable.OnDragStartEvent.AddListener(HandleDragStart);
                _draggable.OnDragEndEvent.AddListener(HandleDragEnd);
            }

            UpdateDisplay();
        }

        // ─── Item Management ────────────────────────────────────────

        /// <summary>Set the item in this slot.</summary>
        public void SetItem(object data, Sprite icon, int quantity = 1)
        {
            _itemData = data;
            _currentSprite = icon;
            _quantity = quantity;

            // Set data on draggable for transport
            if (_draggable != null)
            {
                _draggable.DragData = data;
                _draggable.IsDragEnabled = data != null;
            }

            UpdateDisplay();
            OnItemPlaced?.Invoke(this, data);
        }

        /// <summary>Clear the item from this slot.</summary>
        public void ClearItem()
        {
            object oldData = _itemData;
            _itemData = null;
            _currentSprite = null;
            _quantity = 0;

            if (_draggable != null)
            {
                _draggable.DragData = null;
                _draggable.IsDragEnabled = false;
            }

            UpdateDisplay();

            if (oldData != null)
                OnItemRemoved?.Invoke(this, oldData);
        }

        /// <summary>Update the quantity display.</summary>
        public void SetQuantity(int quantity)
        {
            _quantity = quantity;
            UpdateDisplay();
        }

        // ─── Display ────────────────────────────────────────────────

        private void UpdateDisplay()
        {
            if (_itemIcon != null)
            {
                _itemIcon.sprite = _currentSprite;
                _itemIcon.enabled = _currentSprite != null;
                if (_currentSprite != null)
                    _itemIcon.color = Color.white;
            }

            if (_quantityText != null)
            {
                _quantityText.text = _quantity > 1 ? _quantity.ToString() : "";
                _quantityText.enabled = _quantity > 1;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = _defaultColor;
            }
        }

        // ─── Event Handlers ─────────────────────────────────────────

        private void HandleItemDropped(IDraggable draggable, DragContext context)
        {
            // The draggable carries data — extract it
            if (draggable.DragData != null)
            {
                // Find the source slot (if it came from an inventory slot)
                var sourceSlot = draggable.GameObject.GetComponentInParent<InventorySlot>();
                if (sourceSlot != null && sourceSlot != this)
                {
                    // Transfer data
                    Sprite icon = sourceSlot.CurrentSprite;
                    int qty = sourceSlot.Quantity;
                    object data = sourceSlot.ItemData;

                    sourceSlot.ClearItem();
                    SetItem(data, icon, qty);
                }
                else
                {
                    // External drop — data is just the DragData
                    SetItem(draggable.DragData, null, 1);
                }
            }
        }

        private void HandleHoverEnter(IDraggable draggable, DragContext context)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = HasItem ? _swapHoverColor : _emptyHoverColor;
            }
        }

        private void HandleHoverExit(IDraggable draggable, DragContext context)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _defaultColor;
            }
        }

        private void HandleItemSwapped(IDraggable incoming, IDraggable existing, DragContext context)
        {
            var sourceSlot = incoming.GameObject.GetComponentInParent<InventorySlot>();
            if (sourceSlot != null)
            {
                // Swap data between slots
                object tempData = _itemData;
                Sprite tempSprite = _currentSprite;
                int tempQty = _quantity;

                SetItem(sourceSlot.ItemData, sourceSlot.CurrentSprite, sourceSlot.Quantity);
                sourceSlot.SetItem(tempData, tempSprite, tempQty);

                OnItemSwapped?.Invoke(this, sourceSlot);
            }
        }

        private void HandleDragStart(DragContext context)
        {
            // Optionally dim the slot while item is being dragged
            if (_backgroundImage != null)
            {
                var c = _defaultColor;
                c.a *= 0.5f;
                _backgroundImage.color = c;
            }
        }

        private void HandleDragEnd(DragContext context, DropResult result)
        {
            // Restore slot visual
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _defaultColor;
            }
        }
    }
}
