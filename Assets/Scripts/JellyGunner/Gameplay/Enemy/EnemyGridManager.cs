using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class EnemyGridManager : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private JellyInstanceRenderer _renderer;

        private EnemyBlock[] _enemies;
        private int _capacity;
        private int _activeCount;
        private int _columns;
        private int _rows;
        private float _gridZOffset;
        private float _advanceSpeed;
        private Vector3 _gridOrigin;
        private BlockColor _highlightColor;
        private bool _isHighlighting;

        public int AliveCount { get; private set; }

        public void Initialize(int columns, int rows, Vector3 gridOrigin)
        {
            _columns = columns;
            _rows = rows;
            _gridOrigin = gridOrigin;
            _gridZOffset = 0f;
        }

        public void SpawnWave(LevelData.EnemySpawn[] spawns, float advanceSpeed)
        {
            _advanceSpeed = advanceSpeed;
            _activeCount = spawns.Length;
            AliveCount = _activeCount;
            _capacity = _activeCount;
            _enemies = new EnemyBlock[_capacity];

            for (int i = 0; i < _activeCount; i++)
            {
                var s = spawns[i];
                _enemies[i] = EnemyBlock.Create(
                    s.gridX, s.gridY, s.color, s.tier,
                    GridToWorld(s.gridX, s.gridY)
                );
            }

            SyncToGPU();
        }

        public void Tick(float dt)
        {
            if (_activeCount == 0) return;

            AdvanceGrid(dt);
            UpdateDeformation(dt);
            UpdateDeathAnimations(dt);
            ApplyGravity(dt);
            SyncToGPU();
        }

        public bool TryDamage(int index, int damage)
        {
            if (!IsValidAlive(index)) return false;

            _enemies[index].CurrentHP -= damage;
            _enemies[index].DeformImpact = 1f;

            GameEvents.Publish(new GameEvents.EnemyHit
            {
                EnemyIndex = index,
                Damage = damage
            });

            if (_enemies[index].CurrentHP > 0) return false;

            _enemies[index].CurrentHP = 0;
            _enemies[index].IsDying = true;
            AliveCount--;

            GameEvents.Publish(new GameEvents.EnemyDied
            {
                EnemyIndex = index,
                Color = _enemies[index].Color,
                WorldPosition = _enemies[index].WorldPosition
            });

            if (AliveCount <= 0)
                GameEvents.Publish(new GameEvents.WaveCleared { WaveIndex = 0 });

            return true;
        }

        public int HammerStrike(BlockColor targetColor)
        {
            int killCount = 0;

            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsAlive || _enemies[i].IsDying) continue;
                if (_enemies[i].Color != targetColor) continue;

                _enemies[i].CurrentHP = 0;
                _enemies[i].IsDying = true;
                _enemies[i].DeformImpact = 1f;
                killCount++;

                GameEvents.Publish(new GameEvents.EnemyDied
                {
                    EnemyIndex = i,
                    Color = targetColor,
                    WorldPosition = _enemies[i].WorldPosition
                });
            }

            AliveCount -= killCount;
            if (AliveCount <= 0)
                GameEvents.Publish(new GameEvents.WaveCleared { WaveIndex = 0 });

            return killCount;
        }

        public void SetHighlightColor(BlockColor color, bool active)
        {
            _highlightColor = color;
            _isHighlighting = active;
        }

        public int FindBottomMostByColor(BlockColor color, Vector3 fromPosition)
        {
            int bestIndex = -1;
            int lowestY = int.MaxValue;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < _activeCount; i++)
            {
                if (!IsValidAlive(i)) continue;
                if (_enemies[i].Color != color) continue;

                if (_enemies[i].GridY < lowestY)
                {
                    lowestY = _enemies[i].GridY;
                    bestDistSq = (fromPosition - _enemies[i].WorldPosition).sqrMagnitude;
                    bestIndex = i;
                }
                else if (_enemies[i].GridY == lowestY)
                {
                    float distSq = (fromPosition - _enemies[i].WorldPosition).sqrMagnitude;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestIndex = i;
                    }
                }
            }

            return bestIndex;
        }

        public bool HasAliveOfColor(BlockColor color)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (IsValidAlive(i) && _enemies[i].Color == color)
                    return true;
            }
            return false;
        }

        public bool HasAliveEnemies() => AliveCount > 0;

        public Vector3 GetPosition(int index) =>
            IsValid(index) ? _enemies[index].WorldPosition : Vector3.zero;

        public BlockColor GetColor(int index) =>
            IsValid(index) ? _enemies[index].Color : BlockColor.Red;

        public bool IsAlive(int index) => IsValidAlive(index);

        public int CountAliveOfColor(BlockColor color)
        {
            int count = 0;
            for (int i = 0; i < _activeCount; i++)
            {
                if (IsValidAlive(i) && _enemies[i].Color == color)
                    count++;
            }
            return count;
        }

        private void AdvanceGrid(float dt)
        {
            _gridZOffset -= _advanceSpeed * dt;

            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsAlive) continue;
                _enemies[i].TargetWorldPosition = GridToWorld(_enemies[i].GridX, _enemies[i].GridY);
            }
        }

        private void UpdateDeformation(float dt)
        {
            float decayPow = Mathf.Pow(_config.deformDecayRate, dt * 60f);
            for (int i = 0; i < _activeCount; i++)
            {
                if (_enemies[i].DeformImpact <= 0.01f)
                {
                    _enemies[i].DeformImpact = 0f;
                    continue;
                }
                _enemies[i].DeformImpact *= decayPow;
            }
        }

        private void UpdateDeathAnimations(float dt)
        {
            float deathSpeed = 1f / _config.deathShrinkDuration;
            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsDying) continue;

                _enemies[i].DeathProgress += deathSpeed * dt;
                if (_enemies[i].DeathProgress < 1f) continue;

                _enemies[i].DeathProgress = 1f;
                _enemies[i].IsAlive = false;
                CollapseAbove(_enemies[i].GridX, _enemies[i].GridY);
            }
        }

        private void ApplyGravity(float dt)
        {
            const float gravity = 20f;
            const float snapThresholdSq = 0.002f;

            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsAlive) continue;

                Vector3 diff = _enemies[i].TargetWorldPosition - _enemies[i].WorldPosition;
                if (diff.sqrMagnitude < snapThresholdSq)
                {
                    _enemies[i].WorldPosition = _enemies[i].TargetWorldPosition;
                    _enemies[i].FallVelocity = 0f;
                    continue;
                }

                _enemies[i].FallVelocity += gravity * dt;
                _enemies[i].WorldPosition = Vector3.MoveTowards(
                    _enemies[i].WorldPosition,
                    _enemies[i].TargetWorldPosition,
                    _enemies[i].FallVelocity * dt
                );
            }
        }

        private void CollapseAbove(int gridX, int gridY)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsAlive) continue;
                if (_enemies[i].GridX != gridX || _enemies[i].GridY <= gridY) continue;

                _enemies[i].GridY--;
                _enemies[i].TargetWorldPosition = GridToWorld(_enemies[i].GridX, _enemies[i].GridY);
            }
        }

        private Vector3 GridToWorld(int x, int y)
        {
            float cell = _config.cellSize;
            float halfWidth = (_columns - 1) * cell * 0.5f;
            return new Vector3(
                _gridOrigin.x + x * cell - halfWidth,
                _gridOrigin.y,
                _gridOrigin.z + y * cell + _gridZOffset
            );
        }

        private void OnDrawGizmos()
        {
            if (_enemies == null) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < _activeCount; i++)
            {
                if (!_enemies[i].IsAlive) continue;
                Gizmos.DrawWireCube(_enemies[i].WorldPosition, Vector3.one * _config.cellSize * 0.9f);

                // Visualize target position
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_enemies[i].TargetWorldPosition, _config.cellSize * 0.2f);
                Gizmos.color = Color.red;
            }

            // Visualize grid origin
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_gridOrigin, 0.5f);
        }

        private void SyncToGPU()
        {
            var batch = _renderer.EnemyBatch;
            if (batch?.cpuData == null) return;

            batch.activeCount = _activeCount;

            for (int i = 0; i < _activeCount; i++)
            {
                ref var e = ref _enemies[i];

                float size = e.GridSize * _config.cellSize * 0.45f;
                Vector3 scale = Vector3.one * size;

                float spin = e.IsDying
                    ? e.DeathProgress * 360f / _config.deathSpinSpeed
                    : 0f;

                float highlight = (_isHighlighting && e.IsAlive && !e.IsDying && e.Color == _highlightColor)
                    ? 1f
                    : 0f;

                batch.cpuData[i] = new JellyInstanceData
                {
                    color = _palette.GetColorVector(e.Color),
                    deformImpact = e.DeformImpact,
                    hpNormalized = e.HPNormalized,
                    deathProgress = e.DeathProgress,
                    highlightPulse = highlight
                };
                batch.cpuData[i].SetTransform(e.WorldPosition, Quaternion.Euler(0f, spin, 0f), scale);
            }

            _renderer.MarkDirty(batch);
        }

        private bool IsValid(int i) => i >= 0 && i < _activeCount;
        private bool IsValidAlive(int i) => IsValid(i) && _enemies[i].IsAlive && !_enemies[i].IsDying;
    }
}
