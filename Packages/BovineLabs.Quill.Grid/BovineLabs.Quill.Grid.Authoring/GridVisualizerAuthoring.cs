using BovineLabs.Core.Authoring.EntityCommands;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Authoring
{
    public class GridVisualizerAuthoring : MonoBehaviour
    {
        public float cellSize = 1f;
        public int gridWidth = 32;
        public int gridHeight = 32;
        public Vector3 origin = Vector3.zero;
        public bool enabledByDefault = true;

        public string algorithmName = "Grid";
        public string category = "Grid";

        [Header("Visualization Toggles")] public bool drawGrid = true;

        public bool drawObstacles = true;
        public bool drawFrontier = true;
        public bool drawClosed = true;
        public bool drawPath = true;
        public bool drawLabels = true;
        public bool drawHeatmap = true;
        public bool drawIntervals;
        public bool drawConstraints;
        public bool drawConflicts;
        public bool drawMessages;
        public bool drawVectorField;
        public bool drawTimeline;
    }

    public class GridVisualizerBaker : Baker<GridVisualizerAuthoring>
    {
        public override void Bake(GridVisualizerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var commands = new BakerCommands(this, entity);
            var builder = GridVisualizerAuthoringBuilderExtensions.FromAuthoring(authoring);
            builder.ApplyTo(ref commands);
        }
    }
}
