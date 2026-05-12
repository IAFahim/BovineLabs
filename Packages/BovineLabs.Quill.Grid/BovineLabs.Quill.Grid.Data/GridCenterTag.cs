using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GridCenterTag : IComponentData 
{ 
}

public struct GridVisualizerConfig : IComponentData
{
    public int2 Size;
    public float3 BlockSize;
    public float Spacing;

    public float HoverRadius;
    public float HoverDepth;
    public float4 DefaultColor;
    public float4 HoverColor;

    public float TransitionSpeed;

    public bool RevealEnabled;
    public float RevealYOffset;
    public float4 RevealTextColor;
    public float RevealTextSize;
}

public struct GridVisualizerInput : IComponentData
{
    public float3 HoverWorldPosition;
    public bool IsHovering;
}

public struct GridCellVisualState : IBufferElementData
{
    public float TargetDepth;
    public float CurrentDepth;
    public float4 TargetColor;
    public float4 CurrentColor;
}