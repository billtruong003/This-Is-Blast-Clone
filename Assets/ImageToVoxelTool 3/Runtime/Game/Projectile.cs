using UnityEngine;

namespace ImageToVoxel.Game
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private int targetRangeIndex;
        private float lifetime = 5f;
        private float spawnTime;

        public void Launch(Vector3 dir, float spd, int rangeIndex)
        {
            direction = dir.normalized;
            speed = spd;
            targetRangeIndex = rangeIndex;
            spawnTime = Time.time;

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;

            if (Time.time - spawnTime > lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var block = other.GetComponent<Block>();
            if (block == null || block.IsDestroyed) return;

            if (block.RangeIndex == targetRangeIndex)
            {
                block.TakeHit();
                SpawnHitEffect();
                Destroy(gameObject);
            }
        }

        private void SpawnHitEffect()
        {
            var particles = GetComponentInChildren<ParticleSystem>();
            if (particles == null) return;

            particles.transform.SetParent(null);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + 0.5f);
        }
    }
}
