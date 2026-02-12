using System;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        [SerializeField] private LevelManager levelManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BlastTray blastTray;
        [SerializeField] private DragDropController dragDropController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LevelData[] levels;

        private GameState currentState = GameState.Idle;
        private int currentLevelIndex;
        private int totalBlastsUsed;
        private float levelStartTime;

        public GameState CurrentState => currentState;
        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalBlastsUsed => totalBlastsUsed;
        public float LevelElapsedTime => Time.time - levelStartTime;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnLevelCompleted;
        public event Action<int> OnLevelFailed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            BindEvents();

            if (levels != null && levels.Length > 0)
                LoadLevel(0);
        }

        private void OnDestroy()
        {
            UnbindEvents();
            if (instance == this)
                instance = null;
        }

        public void LoadLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length) return;

            currentLevelIndex = index;
            totalBlastsUsed = 0;
            TransitionTo(GameState.Loading);

            levelManager.LoadLevel(levels[index]);
        }

        public void StartPlaying()
        {
            levelStartTime = Time.time;
            dragDropController.SetInputEnabled(true);
            TransitionTo(GameState.Playing);
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            dragDropController.SetInputEnabled(false);
            Time.timeScale = 0;
            TransitionTo(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            Time.timeScale = 1;
            dragDropController.SetInputEnabled(true);
            TransitionTo(GameState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1;
            LoadLevel(currentLevelIndex);
        }

        public void NextLevel()
        {
            int next = currentLevelIndex + 1;
            if (levels != null && next < levels.Length)
                LoadLevel(next);
        }

        private void TransitionTo(GameState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
            if (uiManager != null)
                uiManager.OnGameStateChanged(newState);
        }

        private void BindEvents()
        {
            if (gridManager != null)
            {
                gridManager.OnAllBlocksCleared += HandleAllBlocksCleared;
                gridManager.OnBlockCountChanged += HandleBlockCountChanged;
            }

            if (blastTray != null)
                blastTray.OnTrayEmpty += HandleTrayEmpty;

            if (dragDropController != null)
                dragDropController.OnBlastPlaced += HandleBlastPlaced;

            if (levelManager != null)
                levelManager.OnLevelReady += HandleLevelReady;
        }

        private void UnbindEvents()
        {
            if (gridManager != null)
            {
                gridManager.OnAllBlocksCleared -= HandleAllBlocksCleared;
                gridManager.OnBlockCountChanged -= HandleBlockCountChanged;
            }

            if (blastTray != null)
                blastTray.OnTrayEmpty -= HandleTrayEmpty;

            if (dragDropController != null)
                dragDropController.OnBlastPlaced -= HandleBlastPlaced;

            if (levelManager != null)
                levelManager.OnLevelReady -= HandleLevelReady;
        }

        private void HandleLevelReady()
        {
            StartPlaying();
        }

        private void HandleAllBlocksCleared()
        {
            dragDropController.SetInputEnabled(false);
            TransitionTo(GameState.Won);
            OnLevelCompleted?.Invoke(currentLevelIndex);
        }

        private void HandleTrayEmpty()
        {
            if (gridManager.RemainingBlocks > 0)
            {
                dragDropController.SetInputEnabled(false);
                TransitionTo(GameState.Failed);
                OnLevelFailed?.Invoke(currentLevelIndex);
            }
        }

        private void HandleBlastPlaced(BlastObject blast, Vector2Int gridPos)
        {
            totalBlastsUsed++;
        }

        private void HandleBlockCountChanged(int remaining)
        {
            if (uiManager != null)
                uiManager.UpdateBlockCount(remaining);
        }
    }

    public enum GameState
    {
        Idle,
        Loading,
        Playing,
        Paused,
        Won,
        Failed
    }
}
