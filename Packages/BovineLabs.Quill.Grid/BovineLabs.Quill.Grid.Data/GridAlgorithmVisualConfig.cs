using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridAlgorithmVisualConfig : IComponentData
    {
        public FixedString64Bytes AlgorithmName;
        public FixedString32Bytes Category;

        public bool DrawGrid;
        public bool DrawObstacles;
        public bool DrawFrontier;
        public bool DrawClosed;
        public bool DrawPath;
        public bool DrawLabels;
        public bool DrawHeatmap;
        public bool DrawIntervals;
        public bool DrawConstraints;
        public bool DrawConflicts;
        public bool DrawMessages;
        public bool DrawVectorField;
        public bool DrawTimeline;
    }
}