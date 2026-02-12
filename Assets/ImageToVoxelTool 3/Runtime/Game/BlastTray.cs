using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class BlastTray : MonoBehaviour
    {
        [SerializeField] private GameObject blastPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float slotSpacing = 1.5f;
        [SerializeField] private Transform trayAnchor;

        private List<BlastSlot> slots = new List<BlastSlot>();

        public bool HasAvailableBlasts => slots.Exists(s => s.RemainingCount > 0);

        public event Action OnTrayEmpty;

        public void SetupTray(LevelData levelData, Dictionary<int, int> blastCounts)
        {
            ClearTray();

            if (trayAnchor == null)
                trayAnchor = transform;

            int slotIndex = 0;
            foreach (var kvp in blastCounts)
            {
                if (kvp.Value <= 0) continue;

                var slot = new BlastSlot
                {
                    RangeIndex = kvp.Key,
                    TotalCount = kvp.Value,
                    RemainingCount = kvp.Value,
                    Color = levelData.GetColor(kvp.Key)
                };

                slots.Add(slot);
                CreateSlotVisual(slot, slotIndex);
                slotIndex++;
            }
        }

        public BlastObject TakeBlast(int rangeIndex)
        {
            var slot = slots.Find(s => s.RangeIndex == rangeIndex && s.RemainingCount > 0);
            if (slot == null) return null;

            slot.RemainingCount--;
            UpdateSlotVisual(slot);

            var blastObj = Instantiate(blastPrefab).GetComponent<BlastObject>();
            if (blastObj == null)
                blastObj = Instantiate(blastPrefab).AddComponent<BlastObject>();

            blastObj.Initialize(rangeIndex, slot.Color, projectilePrefab);

            if (!HasAvailableBlasts)
                OnTrayEmpty?.Invoke();

            return blastObj;
        }

        public int GetRemainingCount(int rangeIndex)
        {
            var slot = slots.Find(s => s.RangeIndex == rangeIndex);
            return slot?.RemainingCount ?? 0;
        }

        public List<int> GetAvailableRanges()
        {
            var available = new List<int>();
            foreach (var slot in slots)
                if (slot.RemainingCount > 0)
                    available.Add(slot.RangeIndex);
            return available;
        }

        public void ClearTray()
        {
            foreach (var slot in slots)
            {
                if (slot.Visual != null)
                    Destroy(slot.Visual);
            }
            slots.Clear();
        }

        private void CreateSlotVisual(BlastSlot slot, int index)
        {
            float xPos = (index - slots.Count * 0.5f) * slotSpacing;
            Vector3 position = trayAnchor.position + new Vector3(xPos, 0, 0);

            var visual = Instantiate(blastPrefab, position, Quaternion.identity, trayAnchor);
            visual.name = $"TraySlot_{slot.RangeIndex}";

            var renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                mat.color = slot.Color;
                renderer.material = mat;
            }

            slot.Visual = visual;

            var collider = visual.GetComponent<Collider>();
            if (collider == null)
                visual.AddComponent<BoxCollider>();

            var trayItem = visual.AddComponent<BlastTrayItem>();
            trayItem.Initialize(slot.RangeIndex, slot.RemainingCount);
        }

        private void UpdateSlotVisual(BlastSlot slot)
        {
            if (slot.Visual == null) return;

            var trayItem = slot.Visual.GetComponent<BlastTrayItem>();
            if (trayItem != null)
                trayItem.UpdateCount(slot.RemainingCount);

            if (slot.RemainingCount <= 0)
            {
                var renderer = slot.Visual.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var color = renderer.material.color;
                    color.a = 0.3f;
                    renderer.material.color = color;
                }
            }
        }

        private class BlastSlot
        {
            public int RangeIndex;
            public int TotalCount;
            public int RemainingCount;
            public Color Color;
            public GameObject Visual;
        }
    }

    public class BlastTrayItem : MonoBehaviour
    {
        private int rangeIndex;
        private int count;
        private TextMesh countLabel;

        public int RangeIndex => rangeIndex;
        public int Count => count;

        public void Initialize(int range, int initialCount)
        {
            rangeIndex = range;
            count = initialCount;
            CreateLabel();
        }

        public void UpdateCount(int newCount)
        {
            count = newCount;
            if (countLabel != null)
                countLabel.text = count.ToString();
        }

        private void CreateLabel()
        {
            var labelObj = new GameObject("CountLabel");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = Vector3.up * 1.2f;
            labelObj.transform.localScale = Vector3.one * 0.3f;

            countLabel = labelObj.AddComponent<TextMesh>();
            countLabel.text = count.ToString();
            countLabel.alignment = TextAlignment.Center;
            countLabel.anchor = TextAnchor.MiddleCenter;
            countLabel.fontSize = 32;
            countLabel.color = Color.white;
        }
    }
}
