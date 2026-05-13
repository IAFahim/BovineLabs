using BovineLabs.Quill.Grid.Data;
using Unity.Collections;

namespace BovineLabs.Quill.Grid.Authoring
{
    public static class GridVisualizerAuthoringBuilderExtensions
    {
        public static GridVisualizerBuilder FromAuthoring(GridVisualizerAuthoring authoring)
        {
            return new GridVisualizerBuilder()
                .WithGlobal(authoring.enabledByDefault, maxFrames: 256)
                .WithGrid(authoring.cellSize, authoring.origin, authoring.gridWidth, authoring.gridHeight)
                .WithConfig(
                    new FixedString64Bytes(authoring.algorithmName),
                    new FixedString32Bytes(authoring.category),
                    authoring.drawGrid,
                    authoring.drawObstacles,
                    authoring.drawFrontier,
                    authoring.drawClosed,
                    authoring.drawPath,
                    authoring.drawLabels,
                    authoring.drawHeatmap,
                    authoring.drawIntervals,
                    authoring.drawConstraints,
                    authoring.drawConflicts,
                    authoring.drawMessages,
                    authoring.drawVectorField,
                    authoring.drawTimeline)
                .WithVisualBuffers();
        }
    }
}
