using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private bool centerGrid = true;

        private int gridWidth;
        private int gridHeight;
        private Block[,] blockGrid;
        private BlastObject[,] blastGrid;
        private Vector3 gridOffset;
        private int remainingBlocks;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        public int RemainingBlocks => remainingBlocks;

        public event Action OnAllBlocksCleared;
        public event Action<int> OnBlockCountChanged;

        public void InitializeGrid(int width, int height, float size)
        {
            gridWidth = width;
            gridHeight = height;
            cellSize = size;

            blockGrid = new Block[width, height];
            blastGrid = new BlastObject[width, height];
            remainingBlocks = 0;

            gridOffset = centerGrid
                ? new Vector3(-(width - 1) * cellSize * 0.5f, 0, -(height - 1) * cellSize * 0.5f)
                : Vector3.zero;
        }

        public void RegisterBlock(Block block)
        {
            var pos = block.GridPosition;
            if (!IsInBounds(pos.x, pos.y)) return;

            blockGrid[pos.x, pos.y] = block;
            block.OnBlockDestroyed += HandleBlockDestroyed;
            remainingBlocks++;
        }

        public bool CanPlaceBlast(Vector2Int gridPos)
        {
            if (!IsInBounds(gridPos.x, gridPos.y)) return false;
            if (blockGrid[gridPos.x, gridPos.y] != null) return false;
            if (blastGrid[gridPos.x, gridPos.y] != null) return false;
            return true;
        }

        public bool PlaceBlast(BlastObject blast, Vector2Int gridPos)
        {
            if (!CanPlaceBlast(gridPos)) return false;

            blastGrid[gridPos.x, gridPos.y] = blast;
            blast.PlaceOnGrid(GridToWorld(gridPos));
            blast.OnBlastCompleted += HandleBlastCompleted;
            blast.Activate();
            return true;
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - gridOffset;
            int x = Mathf.RoundToInt(local.x / cellSize);
            int y = Mathf.RoundToInt(local.z / cellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize) + gridOffset;
        }

        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            var gridPos = WorldToGrid(worldPos);
            return GridToWorld(gridPos);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        public Block GetBlock(int x, int y)
        {
            if (!IsInBounds(x, y)) return null;
            return blockGrid[x, y];
        }

        public List<Block> GetBlocksByRange(int rangeIndex)
        {
            var result = new List<Block>();
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    if (blockGrid[x, y] != null && !blockGrid[x, y].IsDestroyed && blockGrid[x, y].RangeIndex == rangeIndex)
                        result.Add(blockGrid[x, y]);
            return result;
        }

        public Dictionary<int, int> GetRemainingPerRange()
        {
            var counts = new Dictionary<int, int>();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var block = blockGrid[x, y];
                    if (block == null || block.IsDestroyed) continue;

                    if (!counts.ContainsKey(block.RangeIndex))
                        counts[block.RangeIndex] = 0;
                    counts[block.RangeIndex]++;
                }
            }
            return counts;
        }

        private void HandleBlockDestroyed(Block block)
        {
            block.OnBlockDestroyed -= HandleBlockDestroyed;
            remainingBlocks--;
            OnBlockCountChanged?.Invoke(remainingBlocks);

            if (remainingBlocks <= 0)
                OnAllBlocksCleared?.Invoke();
        }

        private void HandleBlastCompleted(BlastObject blast)
        {
            blast.OnBlastCompleted -= HandleBlastCompleted;
        }
    }
}
