using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridIntervalRenderSystem : ISystem
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
            var drawer =
                SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<GridIntervalRenderSystem>("Grid/Intervals");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, intervals) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridIntervalVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawIntervals) continue;

                var converter =
                    new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize, visualizer.GridWidth,
                        visualizer.GridHeight);

                var array = intervals.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var iv = array[i];
                    if (global.Mode == GridVisualizerMode.Step && iv.Frame > global.CurrentFrame) continue;
                    if (config.DrawTimeline && iv.Frame != global.CurrentFrame) continue;

                    var left = converter.GridPoint(iv.XL, iv.Y + 0.5f, 0.1f);
                    var right = converter.GridPoint(iv.XR, iv.Y + 0.5f, 0.1f);

                    var color = iv.IsExpanded ? GridPalette.IntervalExpanded : GridPalette.Interval;
                    drawer.Line(left, right, color);

                    var root3d = converter.GridPoint(iv.Root.x, iv.Root.y, 0.15f);
                    drawer.Point(root3d, 4f, GridPalette.RootPoint);

                    var mid = (left + right) * 0.5f;
                    mid.y += 0.2f;
                    drawer.Line(root3d, mid, new Color(color.r, color.g, color.b, 0.3f));
                }
            }
        }
    }
}