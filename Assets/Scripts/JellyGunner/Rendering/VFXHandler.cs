using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class VFXHandler : MonoBehaviour
    {
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private ParticleSystem _deathVFXPrefab;
        [SerializeField] private int _poolSize = 16;

        private readonly Queue<ParticleSystem> _pool = new();

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var vfx = Instantiate(_deathVFXPrefab, transform);
                vfx.gameObject.SetActive(false);
                _pool.Enqueue(vfx);
            }
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Subscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Unsubscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void HandleEnemyDied(GameEvents.EnemyDied evt)
        {
            SpawnBurst(evt.WorldPosition, _palette.GetColor(evt.Color), 20);
        }

        private void HandleMerge(GameEvents.MergeTriggered evt)
        {
            SpawnBurst(Vector3.zero, _palette.GetColor(evt.Color), 10);
        }

        private void SpawnBurst(Vector3 position, Color color, int count)
        {
            if (_pool.Count == 0) return;

            var vfx = _pool.Dequeue();
            vfx.transform.position = position;
            vfx.gameObject.SetActive(true);

            var main = vfx.main;
            main.startColor = color;

            var emission = vfx.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)count));

            vfx.Play();

            float duration = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(vfx, duration));
        }

        private System.Collections.IEnumerator ReturnToPool(ParticleSystem vfx, float delay)
        {
            yield return new WaitForSeconds(delay);
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfx.gameObject.SetActive(false);
            _pool.Enqueue(vfx);
        }
    }
}
