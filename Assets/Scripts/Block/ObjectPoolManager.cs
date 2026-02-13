using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

[Serializable]
public struct PoolConfig
{
    public GameObject Prefab;
    public int PrewarmCount;
    public int DefaultCapacity;
    public bool AutoExpand;
}

public sealed class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [SerializeField] private List<PoolConfig> _initialPools = new List<PoolConfig>();

    private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
    private readonly Dictionary<int, Pool> _instanceToPoolMap = new Dictionary<int, Pool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var config in _initialPools)
        {
            if (config.Prefab == null) continue;
            CreatePool(config);
        }
    }

    public void CreatePool(PoolConfig config)
    {
        int prefabId = config.Prefab.GetInstanceID();
        if (_pools.ContainsKey(prefabId)) return;

        var poolParent = new GameObject($"Pool_{config.Prefab.name}").transform;
        poolParent.SetParent(transform);

        var pool = new Pool(config, poolParent, this);
        _pools.Add(prefabId, pool);

        pool.Prewarm();
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        int prefabId = prefab.GetInstanceID();

        if (!_pools.TryGetValue(prefabId, out Pool pool))
        {
            var config = new PoolConfig
            {
                Prefab = prefab,
                PrewarmCount = 0,
                DefaultCapacity = 10,
                AutoExpand = true
            };
            CreatePool(config);
            pool = _pools[prefabId];
        }

        return pool.Get(position, rotation, parent);
    }

    public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        GameObject spawnedObj = Spawn(prefab.gameObject, position, rotation, parent);
        return spawnedObj.GetComponent<T>();
    }

    public void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;

        int instanceId = instance.GetInstanceID();
        if (_instanceToPoolMap.TryGetValue(instanceId, out Pool pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance);
        }
    }

    public void RegisterInstance(int instanceId, Pool pool)
    {
        _instanceToPoolMap[instanceId] = pool;
    }

    public void UnregisterInstance(int instanceId)
    {
        _instanceToPoolMap.Remove(instanceId);
    }

    public class Pool
    {
        private readonly Stack<PoolItem> _stack;
        private readonly PoolConfig _config;
        private readonly Transform _root;
        private readonly ObjectPoolManager _manager;
        private readonly int _prefabId;

        public Pool(PoolConfig config, Transform root, ObjectPoolManager manager)
        {
            _config = config;
            _root = root;
            _manager = manager;
            _stack = new Stack<PoolItem>(config.DefaultCapacity);
            _prefabId = config.Prefab.GetInstanceID();
        }

        public void Prewarm()
        {
            if (_config.PrewarmCount <= 0) return;

            for (int i = 0; i < _config.PrewarmCount; i++)
            {
                PoolItem item = CreateNewItem();
                _stack.Push(item);
                item.GameObject.SetActive(false);
            }
        }

        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
        {
            PoolItem item;

            if (_stack.Count > 0)
            {
                item = _stack.Pop();
            }
            else
            {
                if (!_config.AutoExpand)
                {
                    Debug.LogWarning($"Pool for {_config.Prefab.name} is empty and AutoExpand is false.");
                    return null;
                }
                item = CreateNewItem();
            }

            item.Transform.SetPositionAndRotation(position, rotation);
            item.Transform.SetParent(parent);
            item.GameObject.SetActive(true);
            item.Poolable?.OnSpawn();

            return item.GameObject;
        }

        public void Release(GameObject instance)
        {
            if (!_manager._instanceToPoolMap.ContainsKey(instance.GetInstanceID()))
            {
                Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_root);

            var poolable = instance.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            _stack.Push(new PoolItem(instance));
        }

        private PoolItem CreateNewItem()
        {
            GameObject instance = Instantiate(_config.Prefab, _root);
            instance.SetActive(false);

            int instanceId = instance.GetInstanceID();
            _manager.RegisterInstance(instanceId, this);

            return new PoolItem(instance);
        }
    }

    private readonly struct PoolItem
    {
        public readonly GameObject GameObject;
        public readonly Transform Transform;
        public readonly IPoolable Poolable;

        public PoolItem(GameObject gameObject)
        {
            GameObject = gameObject;
            Transform = gameObject.transform;
            Poolable = gameObject.GetComponent<IPoolable>();
        }
    }
}

public static class ObjectPoolExtensions
{
    public static void Despawn(this GameObject gameObject)
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            GameObject.Destroy(gameObject);
        }
    }

    public static void Despawn(this Component component)
    {
        Despawn(component.gameObject);
    }
}