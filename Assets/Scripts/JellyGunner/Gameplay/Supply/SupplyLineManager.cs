using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class SupplyLineManager : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private TraySystem _tray;
        [SerializeField, Required] private BlasterFactory _factory;
        [SerializeField] private Transform _supplyAnchor;
        [SerializeField] private int _displayColumns = 4;
        [SerializeField] private float _columnSpacing = 1.3f;
        [SerializeField] private float _rowSpacing = 1.3f;
        [SerializeField] private int _displayRows = 4;
        [SerializeField] private float _dropAnimDuration = 0.25f;

        private RingBuffer<SupplyEntry> _buffer;
        private Blaster[] _displaySlots;
        private int _displayCapacity;

        private struct SupplyEntry
        {
            public BlockColor Color;
            public BlasterType Type;
            public int Ammo;
        }

        public int RemainingInBuffer => _buffer?.Count ?? 0;

        public int DisplayedCount
        {
            get
            {
                int count = 0;
                if (_displaySlots == null) return 0;
                for (int i = 0; i < _displayCapacity; i++)
                    if (_displaySlots[i] != null) count++;
                return count;
            }
        }

        public int TotalRemaining => RemainingInBuffer + DisplayedCount;

        public void Initialize(LevelData.SupplyEntry[] entries, int displayColumns)
        {
            ClearDisplay();

            _displayColumns = displayColumns;
            _displayCapacity = _displayColumns * _displayRows;
            _displaySlots = new Blaster[_displayCapacity];

            _buffer = new RingBuffer<SupplyEntry>(_config.supplyBufferCapacity);

            foreach (var entry in entries)
            {
                _buffer.TryEnqueue(new SupplyEntry
                {
                    Color = entry.color,
                    Type = entry.type,
                    Ammo = entry.Ammo
                });
            }

            FillDisplay();
        }

        public void OnBlockClicked(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= _displayCapacity) return;
            if (_displaySlots[displayIndex] == null) return;
            if (!_tray.HasEmptySlot) return;

            var blaster = _displaySlots[displayIndex];
            _displaySlots[displayIndex] = null;

            _tray.TryPlaceBlaster(blaster);

            StartCoroutine(ShiftColumnAndRefill(displayIndex));
        }

        public Blaster GetDisplayBlaster(int index)
        {
            if (index < 0 || index >= _displayCapacity) return null;
            return _displaySlots[index];
        }

        public int ResolveDisplayIndex(Blaster blaster)
        {
            if (_displaySlots == null) return -1;
            for (int i = 0; i < _displayCapacity; i++)
            {
                if (_displaySlots[i] == blaster) return i;
            }
            return -1;
        }

        private void FillDisplay()
        {
            for (int i = 0; i < _displayCapacity; i++)
            {
                if (_displaySlots[i] != null) continue;
                if (!_buffer.TryDequeue(out var entry)) break;

                _displaySlots[i] = SpawnBlock(entry, GetDisplayWorldPosition(i));
            }
        }

        private IEnumerator ShiftColumnAndRefill(int removedIndex)
        {
            int col = removedIndex % _displayColumns;

            for (int row = removedIndex / _displayColumns; row < _displayRows - 1; row++)
            {
                int current = row * _displayColumns + col;
                int above = (row + 1) * _displayColumns + col;

                _displaySlots[current] = _displaySlots[above];
                _displaySlots[above] = null;

                if (_displaySlots[current] != null)
                {
                    var target = GetDisplayWorldPosition(current);
                    StartCoroutine(AnimateDrop(_displaySlots[current].transform, target));
                }
            }

            int topSlot = (_displayRows - 1) * _displayColumns + col;
            if (_buffer.TryDequeue(out var entry))
            {
                Vector3 spawnPos = GetDisplayWorldPosition(topSlot) + Vector3.up * _rowSpacing;
                var blaster = SpawnBlock(entry, spawnPos);
                _displaySlots[topSlot] = blaster;
                StartCoroutine(AnimateDrop(blaster.transform, GetDisplayWorldPosition(topSlot)));
            }

            yield return null;
        }

        private IEnumerator AnimateDrop(Transform target, Vector3 endPos)
        {
            if (target == null) yield break;

            Vector3 startPos = target.position;
            float elapsed = 0f;

            while (elapsed < _dropAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _dropAnimDuration;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                target.position = Vector3.Lerp(startPos, endPos, ease);
                yield return null;
            }

            target.position = endPos;
        }

        private Blaster SpawnBlock(SupplyEntry entry, Vector3 position)
        {
            var blaster = _factory.Spawn(entry.Type, entry.Color, entry.Ammo, _supplyAnchor);
            if (blaster == null) return null;

            blaster.transform.position = position;
            blaster.gameObject.layer = gameObject.layer;

            return blaster;
        }

        private Vector3 GetDisplayWorldPosition(int index)
        {
            int col = index % _displayColumns;
            int row = index / _displayColumns;
            float halfWidth = (_displayColumns - 1) * _columnSpacing * 0.5f;

            Vector3 localPos = new Vector3(
                col * _columnSpacing - halfWidth,
                0f,
                row * _rowSpacing
            );

            return _supplyAnchor ? _supplyAnchor.TransformPoint(localPos) : localPos;
        }

        private void ClearDisplay()
        {
            if (_displaySlots == null) return;
            for (int i = 0; i < _displayCapacity; i++)
            {
                if (_displaySlots[i] != null)
                {
                    _factory.Recycle(_displaySlots[i]);
                    _displaySlots[i] = null;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_supplyAnchor == null) return;

            Gizmos.color = Color.green;
            int capacity = _displayColumns * _displayRows;
            float size = _columnSpacing * 0.8f;

            for (int i = 0; i < capacity; i++)
            {
                Vector3 pos = GetDisplayWorldPosition(i);
                Gizmos.DrawWireCube(pos, new Vector3(size, 0.1f, size));

                // Draw arrow to show drop direction (if any)
                // Gizmos.DrawLine(pos, pos + Vector3.back * 0.5f);
            }
        }

        private void OnDestroy() => ClearDisplay();
    }
}
