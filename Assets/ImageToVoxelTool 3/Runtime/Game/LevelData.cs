using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel.Game
{
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Image To Voxel/Level Data")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private VoxelMapData mapData;
        [SerializeField] private ColorConfig[] colorConfigs;
        [SerializeField] private int blocksPerBlast = 20;

        public int LevelIndex => levelIndex;
        public VoxelMapData MapData => mapData;
        public int BlocksPerBlast => blocksPerBlast;
        public int ColorCount => colorConfigs != null ? colorConfigs.Length : 0;

        public ColorConfig GetColorConfig(int rangeIndex)
        {
            if (colorConfigs == null || rangeIndex < 0 || rangeIndex >= colorConfigs.Length)
                return ColorConfig.Default;
            return colorConfigs[rangeIndex];
        }

        public Color GetColor(int rangeIndex)
        {
            return GetColorConfig(rangeIndex).BlockColor;
        }

        public int GetBlastCount(int rangeIndex)
        {
            if (mapData == null) return 0;
            var counts = mapData.CountPerRange();
            if (!counts.ContainsKey(rangeIndex)) return 0;
            return Mathf.CeilToInt((float)counts[rangeIndex] / blocksPerBlast);
        }

        public Dictionary<int, int> GetAllBlastCounts()
        {
            var result = new Dictionary<int, int>();
            if (mapData == null) return result;

            var counts = mapData.CountPerRange();
            foreach (var kvp in counts)
                result[kvp.Key] = Mathf.CeilToInt((float)kvp.Value / blocksPerBlast);

            return result;
        }

        public void InitializeFromMapData()
        {
            if (mapData == null) return;

            colorConfigs = new ColorConfig[mapData.TotalRanges];
            for (int i = 0; i < mapData.TotalRanges; i++)
            {
                colorConfigs[i] = new ColorConfig
                {
                    BlockColor = GenerateDistinctColor(i, mapData.TotalRanges),
                    RangeName = $"Color_{i}"
                };
            }
        }

        private static Color GenerateDistinctColor(int index, int total)
        {
            if (total <= 1) return Color.red;
            float hue = (float)index / total;
            return Color.HSVToRGB(hue, 0.75f, 0.9f);
        }

        [Serializable]
        public struct ColorConfig
        {
            public string RangeName;
            public Color BlockColor;

            public static ColorConfig Default => new ColorConfig
            {
                RangeName = "Unknown",
                BlockColor = Color.gray
            };
        }
    }
}
