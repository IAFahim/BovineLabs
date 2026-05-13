using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridVectorFieldRenderSystem : ISystem
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
                SystemAPI.GetSingleton<DrawSystem.Singleton>()
                    .CreateDrawer<GridVectorFieldRenderSystem>("Grid/VectorField");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, vectors) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridVectorFieldVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawVectorField) continue;

                var converter =
                    new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize, visualizer.GridWidth,
                        visualizer.GridHeight);
                var arrowScale = visualizer.CellSize * 0.4f;

                var array = vectors.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var v = array[i];
                    if (global.Mode == GridVisualizerMode.Step && v.Frame > global.CurrentFrame) continue;
                    if (config.DrawTimeline && v.Frame != global.CurrentFrame) continue;

                    var center = converter.CellCenter(v.Cell);
                    center.y += 0.15f;

                    var dir = new float3(v.Direction.x, 0f, v.Direction.y) * arrowScale * v.Magnitude;
                    var end = center + dir;

                    drawer.Arrow(center, dir, GridPalette.VectorField);
                }
            }
        }
    }
}