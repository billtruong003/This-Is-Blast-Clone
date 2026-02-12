using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class ProgressBarUI : MonoBehaviour
    {
        [SerializeField, Required] private EnemyGridManager _enemyGrid;
        [SerializeField, Required] private SupplyLineManager _supply;
        [SerializeField, Required] private Image _progressFill;
        [SerializeField, Required] private Text _enemyCountText;
        [SerializeField] private Text _supplyCountText;

        private int _totalEnemiesAtStart;

        public void SetTotalEnemies(int total)
        {
            _totalEnemiesAtStart = total;
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Subscribe<GameEvents.WaveCleared>(HandleWaveCleared);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Unsubscribe<GameEvents.WaveCleared>(HandleWaveCleared);
        }

        private void Update()
        {
            if (_supplyCountText)
                _supplyCountText.text = $"Supply: {_supply.TotalRemaining}";
        }

        private void HandleEnemyDied(GameEvents.EnemyDied evt)
        {
            Refresh();
        }

        private void HandleWaveCleared(GameEvents.WaveCleared evt)
        {
            _progressFill.fillAmount = 1f;
        }

        private void Refresh()
        {
            if (_totalEnemiesAtStart <= 0) return;

            int alive = _enemyGrid.AliveCount;
            float progress = 1f - (float)alive / _totalEnemiesAtStart;
            _progressFill.fillAmount = progress;
            _enemyCountText.text = $"{alive}";
        }
    }
}
