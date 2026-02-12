using UnityEngine;
using System.Collections.Generic;

namespace JellyGunner
{
    public class ComponentPool<T> where T : Component
    {
        private readonly Queue<T> _available = new();
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly int _growAmount;

        public int ActiveCount { get; private set; }
        public int TotalCreated { get; private set; }

        public ComponentPool(GameObject prefab, Transform parent, int initialSize, int growAmount = 4)
        {
            _prefab = prefab;
            _parent = parent;
            _growAmount = growAmount;
            Grow(initialSize);
        }

        public T Get()
        {
            if (_available.Count == 0)
                Grow(_growAmount);

            var item = _available.Dequeue();
            item.gameObject.SetActive(true);
            ActiveCount++;
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent);
            _available.Enqueue(item);
            ActiveCount--;
        }

        public void ReturnAll(System.Action<T> beforeReturn = null)
        {
            ActiveCount = 0;
        }

        private void Grow(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                var comp = go.GetComponent<T>();
                _available.Enqueue(comp);
                TotalCreated++;
            }
        }
    }
}
