using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridVisualizerStyle : IComponentData
    {
        public GridVisualStyleType Type;
    }

    public enum GridVisualStyleType : byte
    {
        Flat,
        Pillar,
        HeatmapPlane,
        ScanReveal,
        FlowField,
        CombatThreat
    }
}
