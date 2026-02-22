using UnityEngine;
using System;
using JellyGunner;

public class TargetBlock : MonoBehaviour
{
    public BlockColor ColorType { get; private set; }
    public int HP { get; private set; }

    // Column (Horizontal index) and Depth (Vertical index in the column)
    public int ColumnIndex { get; private set; }
    public int DepthIndex { get; private set; }

    private Action<TargetBlock> _onDestroyed;

    public void Initialize(BlockConfigSO.BlockDefinition config, int col, int depth, Action<TargetBlock> onDestroyedCallback)
    {
        ColorType = config.ColorEnum;
        HP = config.BaseHP;
        ColumnIndex = col;
        DepthIndex = depth;
        _onDestroyed = onDestroyedCallback;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = config.VisualColor;
        }
    }

    public void UpdateGridPosition(int newDepth, Vector3 worldPosition)
    {
        DepthIndex = newDepth;
        LeanTween.cancel(gameObject);
        LeanTween.move(gameObject, worldPosition, 0.3f).setEaseOutQuad();
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
            transform.localScale = Vector3.one * 0.9f;
            LeanTween.scale(gameObject, Vector3.one, 0.2f).setEaseOutBack();
        }
    }

    private void Die()
    {
        _onDestroyed?.Invoke(this);
        LeanTween.cancel(gameObject);

        // Simple death effect before destroying
        transform.localScale = Vector3.zero;
        Destroy(gameObject, 0.1f);
    }
}
