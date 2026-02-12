using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class DeadlockWarningUI : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private Image _warningOverlay;
        [SerializeField, Required] private GameObject _gameOverPanel;
        [SerializeField, Required] private GameObject _victoryPanel;
        [SerializeField, Required] private Text _trayCountText;

        private bool _isWarning;
        private float _flashTimer;

        private void Awake()
        {
            _warningOverlay.gameObject.SetActive(false);
            _gameOverPanel.SetActive(false);
            _victoryPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.GameStateChanged>(HandleStateChanged);
            GameEvents.Subscribe<GameEvents.TrayStateChanged>(HandleTrayChanged);
            GameEvents.Subscribe<GameEvents.NearDeadlockWarning>(HandleNearDeadlock);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.GameStateChanged>(HandleStateChanged);
            GameEvents.Unsubscribe<GameEvents.TrayStateChanged>(HandleTrayChanged);
            GameEvents.Unsubscribe<GameEvents.NearDeadlockWarning>(HandleNearDeadlock);
        }

        private void Update()
        {
            if (!_isWarning) return;

            _flashTimer += Time.deltaTime;
            float alpha = Mathf.Abs(Mathf.Sin(_flashTimer / _config.warningFlashInterval * Mathf.PI)) * 0.3f;
            var color = _warningOverlay.color;
            color.a = alpha;
            _warningOverlay.color = color;
        }

        private void HandleStateChanged(GameEvents.GameStateChanged evt)
        {
            switch (evt.Current)
            {
                case GameState.Deadlock:
                    StopWarning();
                    _gameOverPanel.SetActive(true);
                    break;

                case GameState.Victory:
                    StopWarning();
                    _victoryPanel.SetActive(true);
                    break;

                case GameState.Playing:
                    StopWarning();
                    _gameOverPanel.SetActive(false);
                    _victoryPanel.SetActive(false);
                    break;

                case GameState.NearDeadlock:
                    StartWarning();
                    break;
            }
        }

        private void HandleTrayChanged(GameEvents.TrayStateChanged evt)
        {
            _trayCountText.text = $"{evt.OccupiedSlots}/{evt.TotalSlots}";
        }

        private void HandleNearDeadlock(GameEvents.NearDeadlockWarning evt)
        {
            if (!evt.CanMerge)
                StartWarning();
        }

        private void StartWarning()
        {
            _isWarning = true;
            _flashTimer = 0f;
            _warningOverlay.gameObject.SetActive(true);
            _warningOverlay.color = new Color(1f, 0.1f, 0.1f, 0f);
        }

        private void StopWarning()
        {
            _isWarning = false;
            _warningOverlay.gameObject.SetActive(false);
        }
    }
}
