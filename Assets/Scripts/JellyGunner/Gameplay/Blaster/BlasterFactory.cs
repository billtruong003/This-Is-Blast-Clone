using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class BlasterFactory : MonoBehaviour
    {
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private EnemyGridManager _enemyGrid;
        [SerializeField] private BlasterDefinition[] _definitions;
        [SerializeField] private int _poolSizePerType = 8;

        private readonly Dictionary<BlasterType, Queue<GameObject>> _pools = new();
        private readonly Dictionary<BlasterType, BlasterDefinition> _defMap = new();

        public void Initialize()
        {
            foreach (var def in _definitions)
            {
                _defMap[def.type] = def;
                _pools[def.type] = new Queue<GameObject>();

                for (int i = 0; i < _poolSizePerType; i++)
                {
                    var go = CreateBlasterObject(def);
                    go.SetActive(false);
                    _pools[def.type].Enqueue(go);
                }
            }
        }

        public Blaster Spawn(BlasterType type, BlockColor color, int ammo, Transform parent)
        {
            if (!_defMap.TryGetValue(type, out var def)) return null;

            GameObject go;
            if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                go = pool.Dequeue();
                go.SetActive(true);
            }
            else
            {
                go = CreateBlasterObject(def);
            }

            go.transform.SetParent(parent);
            go.transform.localScale = Vector3.one * def.modelScale;

            var blaster = go.GetComponent<Blaster>();
            blaster.Setup(def, color, ammo, _enemyGrid);

            ApplyColor(go, color);

            return blaster;
        }

        public void Recycle(Blaster blaster)
        {
            if (blaster == null) return;

            var go = blaster.gameObject;
            go.SetActive(false);
            go.transform.SetParent(transform);

            if (_pools.TryGetValue(blaster.Definition.type, out var pool))
                pool.Enqueue(go);
            else
                Destroy(go);
        }

        private GameObject CreateBlasterObject(BlasterDefinition def)
        {
            var go = new GameObject($"Blaster_{def.type}");
            go.transform.SetParent(transform);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = def.mesh;

            var meshRenderer = go.AddComponent<MeshRenderer>();

            var collider = go.AddComponent<BoxCollider>();
            if (def.mesh)
            {
                collider.center = def.mesh.bounds.center;
                collider.size = def.mesh.bounds.size;
            }

            go.AddComponent<Blaster>();

            return go;
        }

        private void ApplyColor(GameObject go, BlockColor color)
        {
            if (!go.TryGetComponent<Renderer>(out var rend)) return;
            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", _palette.GetColor(color));
            rend.SetPropertyBlock(mpb);
        }

        public BlasterDefinition GetDefinition(BlasterType type) =>
            _defMap.TryGetValue(type, out var def) ? def : null;
    }
}
