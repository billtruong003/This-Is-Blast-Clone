using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class AudioHandler : MonoBehaviour
    {
        [Title("SFX Clips")]
        [SerializeField] private AudioClip _sfxPlace;
        [SerializeField] private AudioClip _sfxShoot;
        [SerializeField] private AudioClip _sfxHit;
        [SerializeField] private AudioClip _sfxDeath;
        [SerializeField] private AudioClip _sfxMerge;
        [SerializeField] private AudioClip _sfxHammer;
        [SerializeField] private AudioClip _sfxRunAway;
        [SerializeField] private AudioClip _sfxDeadlock;

        [Title("Settings")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;
        [SerializeField, Range(0f, 0.2f)] private float _pitchVariation = 0.08f;
        [SerializeField] private int _poolSize = 8;

        private readonly Queue<AudioSource> _pool = new();

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _pool.Enqueue(src);
            }
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.BlasterPlaced>(e => Play(_sfxPlace));
            GameEvents.Subscribe<GameEvents.EnemyHit>(e => Play(_sfxHit));
            GameEvents.Subscribe<GameEvents.EnemyDied>(e => Play(_sfxDeath));
            GameEvents.Subscribe<GameEvents.MergeTriggered>(e => Play(_sfxMerge));
            GameEvents.Subscribe<GameEvents.HammerActivated>(e => Play(_sfxHammer));
            GameEvents.Subscribe<GameEvents.BlasterDepleted>(e => Play(_sfxRunAway));
            GameEvents.Subscribe<GameEvents.DeadlockDetected>(e => Play(_sfxDeadlock));
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || _pool.Count == 0) return;

            var src = _pool.Dequeue();
            src.clip = clip;
            src.volume = _volume;
            src.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            src.Play();

            StartCoroutine(ReturnToPool(src, clip.length + 0.1f));
        }

        private System.Collections.IEnumerator ReturnToPool(AudioSource src, float delay)
        {
            yield return new WaitForSeconds(delay);
            _pool.Enqueue(src);
        }
    }
}
