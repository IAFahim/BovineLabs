using BovineLabs.Quill.Grid.Data;
using Unity.Collections;

namespace BovineLabs.Quill.Grid.Authoring.Test
{
    public static class GridSyntheticFieldAuthoringBuilderExtensions
    {
        public static GridVisualizerBuilder FromAuthoring(GridSyntheticFieldAuthoring authoring)
        {
            return new GridVisualizerBuilder()
                .WithGlobal(authoring.enabledByDefault, maxFrames: 1)
                .WithGrid(authoring.cellSize, authoring.origin, authoring.gridWidth, authoring.gridHeight)
                .WithConfig(
                    new FixedString64Bytes(authoring.algorithmName),
                    new FixedString32Bytes(authoring.category),
                    drawGrid: authoring.drawGrid,
                    drawObstacles: authoring.drawObstacles,
                    drawFrontier: false,
                    drawClosed: false,
                    drawPath: authoring.drawPath,
                    drawLabels: authoring.drawLabels,
                    drawHeatmap: authoring.drawHeatmap,
                    drawVectorField: authoring.drawVectorField)
                .WithCellBuffer()
                .WithVectorFieldBuffer()
                .WithTextBuffer();
        }
    }
}
