using UnityEngine;
using System.Collections.Generic;
using JellyGunner;

[CreateAssetMenu(fileName = "BlockConfig", menuName = "JellyGunner/Block Config")]
public class BlockConfigSO : ScriptableObject
{
    [System.Serializable]
    public class BlockDefinition
    {
        public BlockColor ColorEnum;
        public Color VisualColor;
        public GameObject Prefab;
        public int BaseHP = 1;
        
        // Support multi-cell blocks if needed visually or logically
        public Vector2Int Size = Vector2Int.one; 
    }

    [SerializeField] private List<BlockDefinition> _definitions;

    public BlockDefinition GetDefinition(BlockColor color)
    {
        return _definitions.Find(d => d.ColorEnum == color);
    }
}