using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel
{
    [CreateAssetMenu(fileName = "NewVoxelMap", menuName = "Image To Voxel/Voxel Map Data")]
    public class VoxelMapData : ScriptableObject
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int totalRanges;
        [SerializeField] private int[] flatData;

        public int Width => width;
        public int Height => height;
        public int TotalRanges => totalRanges;
        public Vector2Int Resolution => new Vector2Int(width, height);
        public bool HasData => flatData != null && flatData.Length > 0;

        public void Store(int[,] data, int ranges)
        {
            width = data.GetLength(0);
            height = data.GetLength(1);
            totalRanges = ranges;
            flatData = Flatten(data);
        }

        public int GetValue(int x, int y)
        {
            if (!IsInBounds(x, y))
                return 0;
            return flatData[y * width + x];
        }

        public int[,] ToArray()
        {
            var result = new int[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = flatData[y * width + x];
            return result;
        }

        public Color GetColorForValue(int value)
        {
            if (totalRanges <= 1)
                return Color.white;
            float t = value / (float)(totalRanges - 1);
            return Color.Lerp(Color.black, Color.white, t);
        }

        public Dictionary<int, int> CountPerRange()
        {
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < totalRanges; i++)
                counts[i] = 0;

            if (flatData == null) return counts;

            foreach (int val in flatData)
            {
                if (counts.ContainsKey(val))
                    counts[val]++;
            }
            return counts;
        }

        public List<Vector2Int> GetPositionsForRange(int rangeIndex)
        {
            var positions = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (GetValue(x, y) == rangeIndex)
                        positions.Add(new Vector2Int(x, y));
            return positions;
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private int[] Flatten(int[,] source)
        {
            int w = source.GetLength(0);
            int h = source.GetLength(1);
            var flat = new int[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flat[y * w + x] = source[x, y];
            return flat;
        }
    }
}
