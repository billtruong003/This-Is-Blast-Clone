using UnityEngine;
using System;
using JellyGunner;

public class TargetBlock : MonoBehaviour
{
    public BlockColor ColorType { get; private set; }
    public int HP { get; private set; }
    public Vector2Int GridPosition { get; private set; } // Base coordinate (bottom-left)
    public int LayerIndex { get; private set; }
    public Vector2Int Size { get; private set; } // Width x Height in grid cells

    private Action<TargetBlock> _onDestroyed;

    public void Initialize(BlockConfigSO.BlockDefinition config, int x, int y, int layer, Action<TargetBlock> onDestroyedCallback)
    {
        ColorType = config.ColorEnum;
        HP = config.BaseHP;
        Size = config.Size;
        GridPosition = new Vector2Int(x, y);
        LayerIndex = layer;
        _onDestroyed = onDestroyedCallback;

        // Visual Setup
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = config.VisualColor;
        }
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Play hit effect / wobble
            transform.localScale = Vector3.one * 0.9f;
            LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
        }
    }

    private void Die()
    {
        _onDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
}
