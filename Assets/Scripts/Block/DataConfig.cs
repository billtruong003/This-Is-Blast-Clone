using System;
using UnityEngine;
using JellyGunner;

[Serializable]
public struct LayoutConfig
{
    public Vector3 StartPosition;
    public Vector3 ColumnAxis;
    public Vector3 RowAxis;
    public Vector2 Spacing;
    public int MaxColumns;
}

public struct SupplyData
{
    public int ID;
    public int Amount;
    public Color BaseColor;
    public BlockColor ColorEnum;
}
