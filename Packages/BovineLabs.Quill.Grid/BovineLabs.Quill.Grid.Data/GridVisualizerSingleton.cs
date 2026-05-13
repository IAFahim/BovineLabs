using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public enum GridVisualizerMode : byte
    {
        Live,
        Step,
        Snapshot
    }

    public struct GridVisualizerGlobal : IComponentData
    {
        public bool Enabled;
        public int CurrentFrame;
        public int MaxFrames;
        public GridVisualizerMode Mode;
    }

    public struct GridVisualizerData : IComponentData
    {
        public float CellSize;
        public float3 Origin;
        public int GridWidth;
        public int GridHeight;
    }
}