using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridPillarStyleConfig : IComponentData
    {
        public float3 BlockSize;
        public float Spacing;
        public float BaseHeight;
        public float MaxDepth;
        public float TransitionSpeed;

        public float4 BaseColor;
        public float4 OutlineColor;
        public float4 ColdColor;
        public float4 HotColor;
    }
}
