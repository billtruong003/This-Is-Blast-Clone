using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "JellyGunner/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Serializable]
        public struct EnemySpawn
        {
            [HorizontalGroup("Row")] public int gridX;
            [HorizontalGroup("Row")] public int gridY;
            [HorizontalGroup("Row")] public BlockColor color;
            [HorizontalGroup("Row")] public EnemyTier tier;

            public int HP => TierConfig.GetHP(tier);
            public int GridSize => TierConfig.GetGridSize(tier);
        }

        [Serializable]
        public struct SupplyEntry
        {
            [HorizontalGroup("Row")] public BlockColor color;
            [HorizontalGroup("Row")] public BlasterType type;

            public int Ammo => TierConfig.GetAmmo(type);
        }

        [Serializable]
        public struct WaveData
        {
            [TableList] public EnemySpawn[] enemies;
            [TableList] public SupplyEntry[] supply;
            [Range(0.01f, 0.5f)] public float advanceSpeed;
        }

        [Title("Level Info")]
        public int levelIndex;
        public string levelName;

        [Title("Grid")]
        [Range(3, 12)] public int columns = 7;
        [Range(3, 20)] public int rows = 10;
        [Range(3, 5)] public int traySlots = 5;

        [Title("Supply")]
        [Range(3, 6)] public int supplyColumns = 4;

        [Title("Waves")]
        [ListDrawerSettings(ShowFoldout = true)]
        public WaveData[] waves;

        [Title("Hammer")]
        [Range(0, 3)] public int hammerCharges = 1;

        [Title("Validation"), ReadOnly]
        [ShowInInspector]
        public int TotalEnemyHP
        {
            get
            {
                if (waves == null) return 0;
                int total = 0;
                foreach (var wave in waves)
                {
                    if (wave.enemies == null) continue;
                    foreach (var e in wave.enemies) total += e.HP;
                }
                return total;
            }
        }

        [ShowInInspector, ReadOnly]
        public int TotalSupplyAmmo
        {
            get
            {
                if (waves == null) return 0;
                int total = 0;
                foreach (var wave in waves)
                {
                    if (wave.supply == null) continue;
                    foreach (var s in wave.supply) total += s.Ammo;
                }
                return total;
            }
        }

        [ShowInInspector, ReadOnly, GUIColor("BalanceColor")]
        public string BalanceStatus => TotalEnemyHP == TotalSupplyAmmo
            ? $"BALANCED ({TotalEnemyHP})"
            : $"UNBALANCED! Enemy HP: {TotalEnemyHP} vs Supply Ammo: {TotalSupplyAmmo}";

#if UNITY_EDITOR
        private Color BalanceColor() => TotalEnemyHP == TotalSupplyAmmo
            ? new Color(0.2f, 1f, 0.4f)
            : new Color(1f, 0.3f, 0.3f);
#endif
    }
}
