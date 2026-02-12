using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BlastTray blastTray;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private Transform levelRoot;

        private LevelData currentLevel;
        private GridSnapshot currentSnapshot;

        public LevelData CurrentLevel => currentLevel;

        public event Action OnLevelReady;
        public event Action OnLevelCleared;

        public void LoadLevel(LevelData levelData)
        {
            ClearCurrentLevel();
            currentLevel = levelData;

            if (levelData == null || levelData.MapData == null) return;

            var mapData = levelData.MapData;

            gridManager.InitializeGrid(mapData.Width, mapData.Height, mapGenerator.CellSize);
            currentSnapshot = mapGenerator.Generate();

            RegisterAllBlocks();
            SetupBlastTray();

            OnLevelReady?.Invoke();
        }

        public void ClearCurrentLevel()
        {
            mapGenerator.ClearGenerated();
            blastTray.ClearTray();
            currentLevel = null;
            currentSnapshot = null;

            if (levelRoot != null)
            {
                for (int i = levelRoot.childCount - 1; i >= 0; i--)
                    Destroy(levelRoot.GetChild(i).gameObject);
            }
        }

        private void RegisterAllBlocks()
        {
            var blocks = mapGenerator.GetComponentsInChildren<Block>();
            foreach (var block in blocks)
                gridManager.RegisterBlock(block);
        }

        private void SetupBlastTray()
        {
            if (currentLevel == null) return;

            var blastCounts = currentLevel.GetAllBlastCounts();
            blastTray.SetupTray(currentLevel, blastCounts);
        }
    }
}
