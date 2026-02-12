using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [Title("Core References")]
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private LevelData _currentLevel;
        [SerializeField, Required] private JellyInstanceRenderer _renderer;
        [SerializeField, Required] private EnemyGridManager _enemyGrid;
        [SerializeField, Required] private TraySystem _tray;
        [SerializeField, Required] private SupplyLineManager _supply;
        [SerializeField, Required] private BlasterFactory _blasterFactory;
        [SerializeField, Required] private HammerPowerUp _hammer;
        [SerializeField, Required] private ProjectileManager _projectiles;

        [Title("UI")]
        [SerializeField] private ProgressBarUI _progressBar;

        [Title("Meshes")]
        [SerializeField, Required] private Mesh _enemyMesh;
        [SerializeField, Required] private Material _enemyMaterial;
        [SerializeField, Required] private Mesh _projectileMesh;
        [SerializeField, Required] private Material _projectileMaterial;

        [Title("Layout")]
        [SerializeField] private Vector3 _gridOrigin = new Vector3(0f, 2f, 15f);

        [Title("Runtime"), ReadOnly]
        [ShowInInspector] private GameState _state;
        [ShowInInspector] private int _currentWaveIndex;

        public GameState State => _state;

        private void Start()
        {
            InitializeLevel();
        }

        private void InitializeLevel()
        {
            TransitionState(GameState.Loading);

            int maxEnemies = CalculateMaxEnemies();
            int maxProjectiles = 256;

            _renderer.Initialize(
                _enemyMesh, _enemyMaterial, maxEnemies,
                _projectileMesh, _projectileMaterial, maxProjectiles
            );

            _blasterFactory.Initialize();
            _enemyGrid.Initialize(_currentLevel.columns, _currentLevel.rows, _gridOrigin);
            _tray.Initialize(_currentLevel.traySlots);
            _projectiles.Initialize(maxProjectiles);
            _hammer.Initialize(_currentLevel.hammerCharges);

            _currentWaveIndex = 0;
            StartWave(_currentWaveIndex);

            TransitionState(GameState.Playing);
        }

        private void StartWave(int waveIndex)
        {
            if (waveIndex >= _currentLevel.waves.Length)
            {
                TransitionState(GameState.Victory);
                GameEvents.Publish(new GameEvents.LevelComplete());
                return;
            }

            var wave = _currentLevel.waves[waveIndex];
            _enemyGrid.SpawnWave(wave.enemies, wave.advanceSpeed);
            _supply.Initialize(wave.supply, _currentLevel.supplyColumns);

            if (_progressBar)
                _progressBar.SetTotalEnemies(wave.enemies.Length);
        }

        private void Update()
        {
            if (_state != GameState.Playing && _state != GameState.NearDeadlock) return;

            float dt = Time.deltaTime;

            _enemyGrid.Tick(dt);
            _projectiles.Tick(dt);
            _renderer.Render();
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.WaveCleared>(HandleWaveCleared);
            GameEvents.Subscribe<GameEvents.DeadlockDetected>(HandleDeadlock);
            GameEvents.Subscribe<GameEvents.NearDeadlockWarning>(HandleNearDeadlock);
            GameEvents.Subscribe<GameEvents.TrayStateChanged>(HandleTrayChanged);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.WaveCleared>(HandleWaveCleared);
            GameEvents.Unsubscribe<GameEvents.DeadlockDetected>(HandleDeadlock);
            GameEvents.Unsubscribe<GameEvents.NearDeadlockWarning>(HandleNearDeadlock);
            GameEvents.Unsubscribe<GameEvents.TrayStateChanged>(HandleTrayChanged);
        }

        private void HandleWaveCleared(GameEvents.WaveCleared evt)
        {
            _currentWaveIndex++;

            if (_currentWaveIndex >= _currentLevel.waves.Length)
            {
                TransitionState(GameState.Victory);
                GameEvents.Publish(new GameEvents.LevelComplete());
                return;
            }

            StartWave(_currentWaveIndex);
        }

        private void HandleDeadlock(GameEvents.DeadlockDetected evt)
        {
            if (_hammer.HasCharge) return;
            TransitionState(GameState.Deadlock);
        }

        private void HandleNearDeadlock(GameEvents.NearDeadlockWarning evt)
        {
            if (_state == GameState.Playing)
                TransitionState(GameState.NearDeadlock);
        }

        private void HandleTrayChanged(GameEvents.TrayStateChanged evt)
        {
            if (_state == GameState.NearDeadlock && evt.OccupiedSlots < evt.TotalSlots)
                TransitionState(GameState.Playing);
        }

        private void TransitionState(GameState newState)
        {
            if (_state == newState) return;
            var prev = _state;
            _state = newState;

            GameEvents.Publish(new GameEvents.GameStateChanged
            {
                Previous = prev,
                Current = newState
            });
        }

        private int CalculateMaxEnemies()
        {
            int max = 0;
            foreach (var wave in _currentLevel.waves)
            {
                if (wave.enemies != null && wave.enemies.Length > max)
                    max = wave.enemies.Length;
            }
            return Mathf.Max(max, 64);
        }

        [Button("Restart Level"), GUIColor(1f, 0.6f, 0.2f)]
        public void RestartLevel()
        {
            GameEvents.Clear();
            InitializeLevel();
        }
    }
}
