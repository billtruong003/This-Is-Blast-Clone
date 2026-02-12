using System;
using System.Collections;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class BlastObject : MonoBehaviour
    {
        [SerializeField] private int rangeIndex;
        [SerializeField] private int projectilesPerDirection = 5;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileInterval = 0.08f;
        [SerializeField] private GameObject projectilePrefab;

        private bool hasActivated;

        public int RangeIndex => rangeIndex;
        public bool HasActivated => hasActivated;

        public event Action<BlastObject> OnBlastCompleted;

        public void Initialize(int range, Color color, GameObject projPrefab)
        {
            rangeIndex = range;
            projectilePrefab = projPrefab;

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                mat.color = color;
                renderer.material = mat;
            }
        }

        public void Activate()
        {
            if (hasActivated) return;
            hasActivated = true;
            StartCoroutine(FireSequence());
        }

        public void PlaceOnGrid(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        private IEnumerator FireSequence()
        {
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            foreach (var direction in directions)
            {
                for (int i = 0; i < projectilesPerDirection; i++)
                {
                    SpawnProjectile(direction, i);
                    yield return new WaitForSeconds(projectileInterval);
                }
            }

            yield return new WaitForSeconds(0.5f);
            OnBlastCompleted?.Invoke(this);
        }

        private void SpawnProjectile(Vector3 direction, int index)
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            var projectile = proj.GetComponent<Projectile>();

            if (projectile == null)
                projectile = proj.AddComponent<Projectile>();

            projectile.Launch(direction, projectileSpeed, rangeIndex);
        }
    }
}
