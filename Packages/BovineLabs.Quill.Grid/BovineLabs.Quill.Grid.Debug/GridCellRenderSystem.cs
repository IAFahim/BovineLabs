using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridCellRenderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridVisualizerData>();
            state.RequireForUpdate<DrawSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>()
                .CreateDrawer<GridCellRenderSystem>("Grid/Cells");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, cells) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridCellVisual>>())
            {
                if (!global.Enabled) continue;

                var converter = new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize,
                    visualizer.GridWidth, visualizer.GridHeight);

                var array = cells.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var cell = array[i];
                    if (!ShouldDrawLayer(config, cell.Layer)) continue;
                    if (!ShouldDrawFrame(global, config, cell.Frame)) continue;

                    var center = converter.CellCenter(cell.Cell);
                    center.y += 0.05f;

                    var color = SelectColor(cell.Layer, cell.Value);
                    var size = new float3(converter.CellSize * 0.95f, 0.05f, converter.CellSize * 0.95f);
                    drawer.Cuboid(center, quaternion.identity, size, color);
                }
            }
        }

        private static bool ShouldDrawLayer(GridAlgorithmVisualConfig config, byte layer)
        {
            switch (layer)
            {
                case GridCellVisual.LayerObstacle: return config.DrawObstacles;
                case GridCellVisual.LayerFrontier: return config.DrawFrontier;
                case GridCellVisual.LayerClosed: return config.DrawClosed;
                case GridCellVisual.LayerPath: return config.DrawPath;
                case GridCellVisual.LayerHeatmap: return config.DrawHeatmap;
                case GridCellVisual.LayerConflict: return config.DrawConflicts;
                case GridCellVisual.LayerConstraint: return config.DrawConstraints;
                default: return true;
            }
        }

        private static bool ShouldDrawFrame(GridVisualizerGlobal global, GridAlgorithmVisualConfig config, int frame)
        {
            if (global.Mode == GridVisualizerMode.Step && frame > global.CurrentFrame)
                return false;

            return !config.DrawTimeline || frame == global.CurrentFrame;
        }

        private static Color SelectColor(byte layer, float value)
        {
            switch (layer)
            {
                case GridCellVisual.LayerObstacle: return GridPalette.Obstacle;
                case GridCellVisual.LayerFrontier: return GridPalette.Frontier;
                case GridCellVisual.LayerClosed: return GridPalette.Closed;
                case GridCellVisual.LayerPath: return GridPalette.Path;
                case GridCellVisual.LayerHeatmap: return GridPalette.LerpHeatmap(value);
                case GridCellVisual.LayerConflict: return GridPalette.Conflict;
                case GridCellVisual.LayerConstraint: return GridPalette.Constraint;
                default: return Color.magenta;
            }
        }
    }
}
#endif