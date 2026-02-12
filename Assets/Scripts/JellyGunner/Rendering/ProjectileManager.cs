using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class ProjectileManager : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private JellyInstanceRenderer _renderer;

        private Projectile[] _pool;
        private int _capacity;
        private int _activeCount;

        private struct Projectile
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector4 Color;
            public float Lifetime;
            public bool Active;
        }

        public void Initialize(int maxProjectiles)
        {
            _capacity = maxProjectiles;
            _pool = new Projectile[_capacity];
            _activeCount = 0;
        }

        public void Spawn(Vector3 origin, Vector3 target, BlockColor color)
        {
            int slot = FindFreeSlot();
            if (slot < 0) return;

            Vector3 dir = (target - origin).normalized;

            _pool[slot] = new Projectile
            {
                Position = origin,
                Velocity = dir * _config.projectileSpeed,
                Color = _palette.GetColorVector(color),
                Lifetime = 3f,
                Active = true
            };

            _activeCount++;
        }

        public void Tick(float dt)
        {
            int writeIndex = 0;
            var batch = _renderer.ProjectileBatch;
            if (batch?.cpuData == null) return;

            for (int i = 0; i < _capacity; i++)
            {
                if (!_pool[i].Active) continue;

                _pool[i].Position += _pool[i].Velocity * dt;
                _pool[i].Lifetime -= dt;

                if (_pool[i].Lifetime <= 0f)
                {
                    _pool[i].Active = false;
                    _activeCount--;
                    continue;
                }

                if (writeIndex >= batch.cpuData.Length) continue;

                float scale = _config.projectileScale;
                batch.cpuData[writeIndex] = new JellyInstanceData
                {
                    color = _pool[i].Color,
                    deformImpact = 0f,
                    hpNormalized = 1f,
                    deathProgress = 0f,
                    highlightPulse = 0f
                };
                batch.cpuData[writeIndex].SetTransform(
                    _pool[i].Position,
                    Quaternion.identity,
                    Vector3.one * scale
                );
                writeIndex++;
            }

            batch.activeCount = writeIndex;
            _renderer.MarkDirty(batch);
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (!_pool[i].Active) return i;
            }
            return -1;
        }
    }
}
