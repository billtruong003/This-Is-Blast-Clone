using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;


[RequireComponent(typeof(Renderer))]
public class SupplyVisual : MonoBehaviour
{
    [SerializeField] private TextMeshPro _amountText;
    [SerializeField] private float _outlineWidth = 0.02f;
    [SerializeField] private float _hdrIntensity = 2.5f;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Color _cachedBaseColor;

    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int _OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Initialize(SupplyData data)
    {
        _cachedBaseColor = data.BaseColor;
        _amountText.text = data.Amount.ToString();

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_BaseColorID, _cachedBaseColor);
        _propBlock.SetColor(_OutlineColorID, Color.black);
        _propBlock.SetFloat(_OutlineWidthID, _outlineWidth);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetStateInTray()
    {
        Color hdrColor = _cachedBaseColor * _hdrIntensity;
        hdrColor.a = 1f;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_OutlineColorID, hdrColor);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetStateInLayout()
    {
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_OutlineColorID, Color.black);
        _renderer.SetPropertyBlock(_propBlock);
    }
}
