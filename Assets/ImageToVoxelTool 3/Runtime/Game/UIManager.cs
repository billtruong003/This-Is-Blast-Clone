using UnityEngine;
using UnityEngine.UI;

namespace ImageToVoxel.Game
{
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text blocksRemainingLabel;
        [SerializeField] private Text blastsUsedLabel;
        [SerializeField] private Button pauseButton;

        [Header("Level Complete")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private Text completionTimeLabel;
        [SerializeField] private Text completionBonusLabel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button retryButton;

        [Header("Pause")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;

        private void Start()
        {
            BindButtons();
            HideAllPanels();
        }

        public void OnGameStateChanged(GameState state)
        {
            HideAllPanels();

            switch (state)
            {
                case GameState.Playing:
                    ShowHUD();
                    break;
                case GameState.Won:
                    ShowLevelComplete();
                    break;
                case GameState.Failed:
                    ShowGameOver();
                    break;
                case GameState.Paused:
                    ShowPause();
                    break;
            }
        }

        public void UpdateBlockCount(int remaining)
        {
            if (blocksRemainingLabel != null)
                blocksRemainingLabel.text = $"Blocks: {remaining}";
        }

        public void UpdateBlastsUsed(int count)
        {
            if (blastsUsedLabel != null)
                blastsUsedLabel.text = $"Blasts: {count}";
        }

        public void SetLevelLabel(int levelIndex)
        {
            if (levelLabel != null)
                levelLabel.text = $"Level {levelIndex + 1}";
        }

        private void ShowHUD()
        {
            SetActive(hudPanel, true);
            SetLevelLabel(GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 0);
        }

        private void ShowLevelComplete()
        {
            SetActive(levelCompletePanel, true);

            if (completionTimeLabel != null && GameManager.Instance != null)
                completionTimeLabel.text = $"Time: {GameManager.Instance.LevelElapsedTime:F1}s";
        }

        private void ShowGameOver()
        {
            SetActive(gameOverPanel, true);
        }

        private void ShowPause()
        {
            SetActive(hudPanel, true);
            SetActive(pausePanel, true);
        }

        private void HideAllPanels()
        {
            SetActive(hudPanel, false);
            SetActive(levelCompletePanel, false);
            SetActive(gameOverPanel, false);
            SetActive(pausePanel, false);
        }

        private void BindButtons()
        {
            BindButton(pauseButton, () => GameManager.Instance?.PauseGame());
            BindButton(resumeButton, () => GameManager.Instance?.ResumeGame());
            BindButton(nextLevelButton, () => GameManager.Instance?.NextLevel());
            BindButton(replayButton, () => GameManager.Instance?.RestartLevel());
            BindButton(retryButton, () => GameManager.Instance?.RestartLevel());
            BindButton(restartButton, () => GameManager.Instance?.RestartLevel());
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void SetActive(GameObject obj, bool active)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
