using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class TargetLayoutManager : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private JellyInstanceRenderer _renderer;

        public class TargetEntity
        {
            public int ID;
            public BlockColor Color;
            public EnemyTier Tier;
            public int MaxHP;
            public int CurrentHP;
            public List<Vector2Int> OccupiedCells = new();
            public Vector3 BasePosition;
            public Vector3 VisualPosition;
            public float DeformImpact;
            public float DeathProgress;
            public bool IsAlive = true;
            public bool IsDying = false;

            public int GridSize => TierConfig.GetGridSize(Tier);
            public int LayerDepth => GridSize > 1 ? 2 : 1;
            public float HPNormalized => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
        }

        private TargetEntity[,] _grid;
        private List<TargetEntity> _entities = new();

        private int _columns;
        private int _rows;
        private Vector3 _gridOrigin;
        private float _gridZOffset;
        private float _advanceSpeed;
        private BlockColor _highlightColor;
        private bool _isHighlighting;

        public int AliveCount { get; private set; }

        public void Initialize(int columns, int rows, Vector3 gridOrigin)
        {
            _columns = columns;
            _rows = rows;
            _gridOrigin = gridOrigin;
            _grid = new TargetEntity[columns, rows];
        }

        public void SpawnWave(LevelData.EnemySpawn[] spawns, float advanceSpeed)
        {
            _entities.Clear();
            _grid = new TargetEntity[_columns, _rows]; // Reset grid
            _advanceSpeed = advanceSpeed;

            foreach (var spawn in spawns)
            {
                CreateTarget(spawn);
            }

            AliveCount = _entities.Count;
            SyncToGPU();
        }

        private void CreateTarget(LevelData.EnemySpawn spawn)
        {
            int size = TierConfig.GetGridSize(spawn.tier);

            if (spawn.gridX < 0 || spawn.gridY < 0 ||
                spawn.gridX + size > _columns || spawn.gridY + size > _rows)
            {
                Debug.LogWarning($"Target at {spawn.gridX},{spawn.gridY} size {size} out of bounds!");
                return;
            }

            // Check overlap
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (_grid[spawn.gridX + x, spawn.gridY + y] != null)
                    {
                        Debug.LogWarning($"Target at {spawn.gridX},{spawn.gridY} overlaps existing target!");
                        return;
                    }
                }
            }

            var entity = new TargetEntity
            {
                ID = _entities.Count,
                Color = spawn.color,
                Tier = spawn.tier,
                MaxHP = TierConfig.GetHP(spawn.tier),
                CurrentHP = TierConfig.GetHP(spawn.tier),
                IsAlive = true,
                VisualPosition = CalculateWorldPosition(spawn.gridX, spawn.gridY, size)
            };
            entity.BasePosition = entity.VisualPosition;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int gx = spawn.gridX + x;
                    int gy = spawn.gridY + y;
                    _grid[gx, gy] = entity;
                    entity.OccupiedCells.Add(new Vector2Int(gx, gy));
                }
            }

            _entities.Add(entity);
        }

        public void Tick(float dt)
        {
            if (_entities.Count == 0) return;

            _gridZOffset -= _advanceSpeed * dt;

            float decayPow = Mathf.Pow(_config.deformDecayRate, dt * 60f);
            float deathSpeed = 1f / _config.deathShrinkDuration;

            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                var entity = _entities[i];

                // Position Update
                if (entity.IsAlive)
                {
                    // Recalculate base position based on current grid scrolling
                    if (entity.OccupiedCells.Count > 0)
                    {
                        var anchor = entity.OccupiedCells[0]; // Use first cell as anchor
                        Vector3 targetPos = CalculateWorldPosition(anchor.x, anchor.y, entity.GridSize);
                        entity.VisualPosition = Vector3.Lerp(entity.VisualPosition, targetPos, dt * 10f);
                    }
                }

                // Deform
                if (entity.DeformImpact > 0.01f) entity.DeformImpact *= decayPow;
                else entity.DeformImpact = 0f;

                // Death
                if (entity.IsDying)
                {
                    entity.DeathProgress += deathSpeed * dt;
                    if (entity.DeathProgress >= 1f)
                    {
                        entity.DeathProgress = 1f;
                        entity.IsAlive = false;
                        // Entities are not removed from list immediately to keep indices valid during frame? 
                        // Actually, we use object references now, so list removal is fine but might affect iteration.
                        // We keep them in list but mark dead.
                        RemoveFromGrid(entity);
                    }
                }
            }

            SyncToGPU();
        }

        private void RemoveFromGrid(TargetEntity entity)
        {
            foreach (var cell in entity.OccupiedCells)
            {
                if (_grid[cell.x, cell.y] == entity)
                    _grid[cell.x, cell.y] = null;
            }
        }

        public bool TryDamage(TargetEntity entity, int damage)
        {
            if (entity == null || !entity.IsAlive || entity.IsDying) return false;

            entity.CurrentHP -= damage;
            entity.DeformImpact = 1f;

            GameEvents.Publish(new GameEvents.EnemyHit { Damage = damage });

            if (entity.CurrentHP <= 0)
            {
                entity.CurrentHP = 0;
                entity.IsDying = true;
                AliveCount--;

                GameEvents.Publish(new GameEvents.EnemyDied
                {
                    Color = entity.Color,
                    WorldPosition = entity.VisualPosition
                });

                if (AliveCount <= 0)
                    GameEvents.Publish(new GameEvents.WaveCleared { WaveIndex = 0 });

                return true;
            }

            return false;
        }

        public TargetEntity FindBottomMostByColor(BlockColor color)
        {
            TargetEntity best = null;
            int lowestY = int.MaxValue;

            // Iterate all entities to find lowest Y
            // Since we have big blocks, "Lowest Y" is defined by the minimum Y of occupied cells

            foreach (var entity in _entities)
            {
                if (!entity.IsAlive || entity.IsDying) continue;
                if (entity.Color != color) continue;

                int entityMinY = int.MaxValue;
                foreach (var cell in entity.OccupiedCells)
                    if (cell.y < entityMinY) entityMinY = cell.y;

                if (entityMinY < lowestY)
                {
                    lowestY = entityMinY;
                    best = entity;
                }
            }
            return best;
        }

        public bool IsAlive(TargetEntity entity) => entity != null && entity.IsAlive && !entity.IsDying;

        public void SetHighlightColor(BlockColor color, bool active)
        {
            _highlightColor = color;
            _isHighlighting = active;
        }

        public int HammerStrike(BlockColor targetColor)
        {
            int killCount = 0;
            foreach (var entity in _entities)
            {
                if (!entity.IsAlive || entity.IsDying) continue;
                if (entity.Color != targetColor) continue;

                entity.CurrentHP = 0;
                entity.IsDying = true;
                entity.DeformImpact = 1f;
                AliveCount--;
                killCount++;

                GameEvents.Publish(new GameEvents.EnemyDied { Color = targetColor, WorldPosition = entity.VisualPosition });
            }

            if (AliveCount <= 0) GameEvents.Publish(new GameEvents.WaveCleared { WaveIndex = 0 });
            return killCount;
        }

        private Vector3 CalculateWorldPosition(int gridX, int gridY, int size)
        {
            float cell = _config.cellSize;
            float halfWidth = (_columns - 1) * cell * 0.5f;

            // Center of the block
            float offset = (size - 1) * cell * 0.5f;

            return new Vector3(
                _gridOrigin.x + gridX * cell - halfWidth + offset,
                _gridOrigin.y,
                _gridOrigin.z + gridY * cell + _gridZOffset + offset
            );
        }

        private void SyncToGPU()
        {
            var batch = _renderer.EnemyBatch;
            if (batch?.cpuData == null) return;

            int activeIndex = 0;
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                // Skip completely dead and animated out
                if (!e.IsAlive && e.DeathProgress >= 1f) continue;

                if (activeIndex >= batch.cpuData.Length) break;

                float size = e.GridSize * _config.cellSize * 0.45f;
                // Layer Logic: Big blocks are deeper/thicker. 
                // Z-scale increased for big blocks to represent "2 layers"
                float zScale = size * (e.LayerDepth > 1 ? 2.5f : 1f);

                Vector3 scale = new Vector3(size, size, zScale);

                float spin = e.IsDying ? e.DeathProgress * 360f / _config.deathSpinSpeed : 0f;
                float highlight = (_isHighlighting && e.IsAlive && !e.IsDying && e.Color == _highlightColor) ? 1f : 0f;

                batch.cpuData[activeIndex] = new JellyInstanceData
                {
                    color = _palette.GetColorVector(e.Color),
                    deformImpact = e.DeformImpact,
                    hpNormalized = e.HPNormalized,
                    deathProgress = e.DeathProgress,
                    highlightPulse = highlight
                };
                batch.cpuData[activeIndex].SetTransform(e.VisualPosition, Quaternion.Euler(0f, spin, 0f), scale);

                activeIndex++;
            }
            batch.activeCount = activeIndex;
            _renderer.MarkDirty(batch);
        }
    }
}