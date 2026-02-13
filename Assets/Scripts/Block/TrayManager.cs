using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

public class TrayManager : MonoBehaviour
{
    [SerializeField] private Transform[] _traySlots;
    private SupplyItem[] _slottedItems;

    private void Awake()
    {
        _slottedItems = new SupplyItem[_traySlots.Length];
    }

    public bool HasSpace()
    {
        return Array.Exists(_slottedItems, x => x == null);
    }

    public void AddSupply(SupplyItem item)
    {
        int slotIndex = Array.FindIndex(_slottedItems, x => x == null);
        if (slotIndex == -1) return;

        _slottedItems[slotIndex] = item;
        item.transform.SetParent(_traySlots[slotIndex]);

        item.MoveToTray(_traySlots[slotIndex].position, () =>
        {
            CheckMatchLogic();
        });
    }

    private void CheckMatchLogic()
    {
        // Future logic for matching 3, merging, etc.
    }
}