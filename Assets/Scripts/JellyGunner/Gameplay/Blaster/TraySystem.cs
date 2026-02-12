using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class TraySystem : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private EnemyGridManager _enemyGrid;
        [SerializeField] private Transform _trayAnchor;
        [SerializeField] private float _slotSpacing = 1.5f;

        private TraySlot[] _slots;
        private int _slotCount;
        private readonly List<int> _mergeWorkList = new(3);

        public int SlotCount => _slotCount;
        public int OccupiedCount { get; private set; }
        public bool HasEmptySlot => OccupiedCount < _slotCount;

        public struct TraySlot
        {
            public Blaster Blaster;
            public bool IsOccupied;

            public BlockColor Color => Blaster != null ? Blaster.Color : BlockColor.Red;
            public int Ammo => Blaster != null ? Blaster.CurrentAmmo : 0;
        }

        public void Initialize(int slotCount)
        {
            _slotCount = slotCount;
            _slots = new TraySlot[slotCount];
            OccupiedCount = 0;
        }

        public bool TryPlaceBlaster(Blaster blaster)
        {
            int freeSlot = FindFirstEmpty();
            if (freeSlot < 0) return false;

            PlaceInSlot(freeSlot, blaster);

            if (TryExecuteMerge(blaster.Color))
                return true;

            EvaluateTrayState();
            return true;
        }

        public Blaster GetBlasterAt(int index)
        {
            if (index < 0 || index >= _slotCount) return null;
            return _slots[index].Blaster;
        }

        public TraySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slotCount) return default;
            return _slots[index];
        }

        private void PlaceInSlot(int slotIndex, Blaster blaster)
        {
            _slots[slotIndex] = new TraySlot { Blaster = blaster, IsOccupied = true };
            blaster.transform.SetParent(_trayAnchor);
            blaster.FlyToSlot(GetSlotWorldPosition(slotIndex), _config.blasterFlyDuration);
            OccupiedCount++;

            GameEvents.Publish(new GameEvents.BlasterPlaced
            {
                SlotIndex = slotIndex,
                Type = blaster.Definition.type,
                Color = blaster.Color,
                Ammo = blaster.CurrentAmmo
            });

            PublishTrayState();
        }

        private bool TryExecuteMerge(BlockColor color)
        {
            _mergeWorkList.Clear();

            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsOccupied) continue;
                if (_slots[i].Color != color) continue;
                _mergeWorkList.Add(i);
            }

            if (_mergeWorkList.Count < 3) return false;

            int targetSlot = _mergeWorkList[0];
            int sourceA = _mergeWorkList[1];
            int sourceB = _mergeWorkList[2];

            int totalAmmo = _slots[targetSlot].Ammo
                          + _slots[sourceA].Ammo
                          + _slots[sourceB].Ammo;

            _slots[sourceA].Blaster.BeginMergeInto(GetSlotWorldPosition(targetSlot), _config.mergeFlyDuration);
            _slots[sourceB].Blaster.BeginMergeInto(GetSlotWorldPosition(targetSlot), _config.mergeFlyDuration);

            ClearSlot(sourceA);
            ClearSlot(sourceB);

            _slots[targetSlot].Blaster.ApplyMergeAmmo(totalAmmo);

            GameEvents.Publish(new GameEvents.MergeTriggered
            {
                TargetSlotIndex = targetSlot,
                SourceSlotA = sourceA,
                SourceSlotB = sourceB,
                Color = color,
                TotalAmmo = totalAmmo
            });

            EvaluateTrayState();
            return true;
        }

        public void OnBlasterDepleted(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotCount) return;
            ClearSlot(slotIndex);
            PublishTrayState();
            EvaluateTrayState();
        }

        private void ClearSlot(int index)
        {
            _slots[index] = default;
            OccupiedCount = Mathf.Max(0, OccupiedCount - 1);
        }

        private void EvaluateTrayState()
        {
            if (!_enemyGrid.HasAliveEnemies()) return;

            int emptySlots = _slotCount - OccupiedCount;

            if (emptySlots > 0) return;

            bool canMerge = CheckCanMerge();
            bool canShoot = CheckCanShoot();

            if (canMerge || canShoot) 
            {
                if (!canShoot)
                {
                    GameEvents.Publish(new GameEvents.NearDeadlockWarning
                    {
                        EmptySlots = 0,
                        CanMerge = canMerge
                    });
                }
                return;
            }

            GameEvents.Publish(new GameEvents.DeadlockDetected());
        }

        private bool CheckCanMerge()
        {
            int[] colorCount = new int[(int)BlockColor.Count];

            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsOccupied) continue;
                colorCount[(int)_slots[i].Color]++;
            }

            for (int c = 0; c < (int)BlockColor.Count; c++)
            {
                if (colorCount[c] >= 3) return true;
            }

            return false;
        }

        private bool CheckCanShoot()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsOccupied) continue;
                if (_slots[i].Blaster == null || _slots[i].Blaster.IsEmpty) continue;

                if (_enemyGrid.HasAliveOfColor(_slots[i].Color))
                    return true;
            }

            return false;
        }

        public bool IsNearDeadlock()
        {
            int empty = _slotCount - OccupiedCount;
            if (empty > 1) return false;
            if (empty == 0) return !CheckCanMerge() || !CheckCanShoot();
            return !CheckCanShoot();
        }

        private int FindFirstEmpty()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsOccupied) return i;
            }
            return -1;
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            float totalWidth = (_slotCount - 1) * _slotSpacing;
            float startX = -totalWidth * 0.5f;
            Vector3 localPos = new Vector3(startX + slotIndex * _slotSpacing, 0f, 0f);
            return _trayAnchor ? _trayAnchor.TransformPoint(localPos) : localPos;
        }

        private void PublishTrayState()
        {
            GameEvents.Publish(new GameEvents.TrayStateChanged
            {
                OccupiedSlots = OccupiedCount,
                TotalSlots = _slotCount
            });
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.BlasterDepleted>(HandleDepleted);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.BlasterDepleted>(HandleDepleted);
        }

        private void HandleDepleted(GameEvents.BlasterDepleted evt)
        {
            OnBlasterDepleted(evt.SlotIndex);
        }

        private void OnDrawGizmos()
        {
            if (_trayAnchor == null) return;
            
            Gizmos.color = Color.cyan;
            int count = _slotCount > 0 ? _slotCount : 5;
            
            float totalWidth = (count - 1) * _slotSpacing;
            float startX = -totalWidth * 0.5f;
            
            for (int i = 0; i < count; i++)
            {
                Vector3 localPos = new Vector3(startX + i * _slotSpacing, 0f, 0f);
                Vector3 pos = _trayAnchor.TransformPoint(localPos);
                Gizmos.DrawWireSphere(pos, 0.4f);
            }
        }
    }
}
