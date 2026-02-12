using System;
using UnityEngine;

namespace ImageToVoxel.Game
{
    public class Block : MonoBehaviour
    {
        [SerializeField] private int rangeIndex;
        [SerializeField] private Vector2Int gridPosition;

        private Renderer blockRenderer;
        private bool isDestroyed;

        public int RangeIndex => rangeIndex;
        public Vector2Int GridPosition => gridPosition;
        public bool IsDestroyed => isDestroyed;

        public event Action<Block> OnBlockDestroyed;

        public void Initialize(int range, Vector2Int position)
        {
            rangeIndex = range;
            gridPosition = position;
            blockRenderer = GetComponentInChildren<Renderer>();
        }

        public void SetColor(Color color)
        {
            if (blockRenderer == null)
                blockRenderer = GetComponentInChildren<Renderer>();
            if (blockRenderer == null) return;

            blockRenderer.material.color = color;
        }

        public void TakeHit()
        {
            if (isDestroyed) return;
            isDestroyed = true;
            OnBlockDestroyed?.Invoke(this);
            PlayDestroyEffect();
        }

        private void PlayDestroyEffect()
        {
            var seq = LeanTweenLite.Scale(gameObject, Vector3.zero, 0.25f);
            Destroy(gameObject, 0.3f);
        }
    }

    public static class LeanTweenLite
    {
        public static Coroutine Scale(GameObject target, Vector3 to, float duration)
        {
            return target.GetComponent<MonoBehaviour>()?.StartCoroutine(ScaleRoutine(target.transform, to, duration));
        }

        private static System.Collections.IEnumerator ScaleRoutine(Transform target, Vector3 to, float duration)
        {
            Vector3 from = target.localScale;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                target.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            target.localScale = to;
        }
    }
}
