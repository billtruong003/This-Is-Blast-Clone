using System;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGunner
{
    public static class GameEvents
    {
        private static readonly Dictionary<Type, Delegate> Listeners = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (Listeners.TryGetValue(type, out var existing))
                Listeners[type] = Delegate.Combine(existing, handler);
            else
                Listeners[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!Listeners.TryGetValue(type, out var existing)) return;

            var result = Delegate.Remove(existing, handler);
            if (result == null) Listeners.Remove(type);
            else Listeners[type] = result;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (Listeners.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(evt);
        }

        public static void Clear() => Listeners.Clear();

        public struct EnemyHit
        {
            public int EnemyIndex;
            public int Damage;
        }

        public struct EnemyDied
        {
            public int EnemyIndex;
            public BlockColor Color;
            public Vector3 WorldPosition;
        }

        public struct BlasterPlaced
        {
            public int SlotIndex;
            public BlasterType Type;
            public BlockColor Color;
            public int Ammo;
        }

        public struct BlasterDepleted
        {
            public int SlotIndex;
        }

        public struct MergeTriggered
        {
            public int TargetSlotIndex;
            public int SourceSlotA;
            public int SourceSlotB;
            public BlockColor Color;
            public int TotalAmmo;
        }

        public struct MergeCompleted
        {
            public int SlotIndex;
            public int NewAmmo;
        }

        public struct NearDeadlockWarning
        {
            public int EmptySlots;
            public bool CanMerge;
        }

        public struct DeadlockDetected { }

        public struct WaveCleared
        {
            public int WaveIndex;
        }

        public struct LevelComplete { }

        public struct HammerActivated
        {
            public BlockColor TargetColor;
            public int EnemiesKilled;
        }

        public struct HammerHoverChanged
        {
            public BlockColor HoverColor;
            public bool IsHovering;
        }

        public struct GridAdvanced
        {
            public float NewZPosition;
        }

        public struct TrayStateChanged
        {
            public int OccupiedSlots;
            public int TotalSlots;
        }

        public struct GameStateChanged
        {
            public GameState Previous;
            public GameState Current;
        }

        public struct SupplyBlockSelected
        {
            public BlockColor Color;
            public BlasterType Type;
            public int Ammo;
        }
    }
}
