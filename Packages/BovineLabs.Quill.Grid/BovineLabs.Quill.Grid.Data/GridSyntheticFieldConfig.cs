using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridSyntheticFieldConfig : IComponentData
    {
        public float BaseWaveStrength;
        public float MemberFalloffPower;
        public float MemberMotionSpeed;
        public bool AnimateMembers;
        public bool WriteVectorField;
        public bool WriteLabels;
    }

    public struct GridSyntheticMember : IBufferElementData
    {
        public float2 BasePosition;
        public float Radius;
        public float Strength;
        public float MotionRadius;
        public float Phase;
        public float4 Color;
    }

    

    public struct GridCellVisualState : IBufferElementData
    {
        public float TargetDepth;
        public float CurrentDepth;
        public float4 TargetColor;
        public float4 CurrentColor;
    }
}