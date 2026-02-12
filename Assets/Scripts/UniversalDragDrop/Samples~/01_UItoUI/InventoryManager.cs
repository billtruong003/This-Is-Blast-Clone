using UnityEngine;
using System.Collections.Generic;

namespace Universal.DragDrop.Samples
{
    /// <summary>
    /// Example: Inventory system with drag-and-drop item management.
    /// Manages a grid of InventorySlots with item data.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private int _slotCount = 20;

        [Header("Test Items")]
        [SerializeField] private ItemDefinition[] _testItems;

        private List<InventorySlot> _slots = new List<InventorySlot>();

        private void Start()
        {
            // Create slots
            for (int i = 0; i < _slotCount; i++)
            {
                var slotGO = Instantiate(_slotPrefab, _slotContainer);
                slotGO.name = $"Slot_{i}";

                var slot = slotGO.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    slot.OnItemPlaced.AddListener((s, data) => OnItemMoved(s, data));
                    slot.OnItemSwapped.AddListener((s1, s2) => OnItemsSwapped(s1, s2));
                    _slots.Add(slot);
                }
            }

            // Add test items
            if (_testItems != null)
            {
                for (int i = 0; i < _testItems.Length && i < _slots.Count; i++)
                {
                    var item = _testItems[i];
                    if (item != null)
                    {
                        _slots[i].SetItem(item, item.Icon, item.StackSize);
                    }
                }
            }
        }

        /// <summary>Add an item to the first available slot.</summary>
        public bool AddItem(ItemDefinition item, int quantity = 1)
        {
            // First try to stack with existing
            foreach (var slot in _slots)
            {
                if (slot.HasItem && slot.ItemData is ItemDefinition existing)
                {
                    if (existing.Id == item.Id && existing.IsStackable)
                    {
                        slot.SetQuantity(slot.Quantity + quantity);
                        return true;
                    }
                }
            }

            // Find empty slot
            foreach (var slot in _slots)
            {
                if (!slot.HasItem)
                {
                    slot.SetItem(item, item.Icon, quantity);
                    return true;
                }
            }

            Debug.Log("Inventory full!");
            return false;
        }

        /// <summary>Remove an item from a specific slot.</summary>
        public ItemDefinition RemoveItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return null;

            var slot = _slots[slotIndex];
            if (!slot.HasItem) return null;

            var item = slot.ItemData as ItemDefinition;
            slot.ClearItem();
            return item;
        }

        private void OnItemMoved(InventorySlot slot, object data)
        {
            Debug.Log($"Item placed in slot {slot.SlotIndex}: {data}");
        }

        private void OnItemsSwapped(InventorySlot slot1, InventorySlot slot2)
        {
            Debug.Log($"Items swapped between slot {slot1.SlotIndex} and {slot2.SlotIndex}");
        }
    }

    /// <summary>
    /// Example item definition. Replace with your own item system.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Universal DragDrop/Sample Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public string Description;
        public int StackSize = 1;
        public bool IsStackable = false;

        [Header("Stats")]
        public int Value;
        public float Weight;

        public override string ToString() => DisplayName;
    }
}
