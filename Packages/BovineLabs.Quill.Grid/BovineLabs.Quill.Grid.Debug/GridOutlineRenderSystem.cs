using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridOutlineRenderSystem : ISystem
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
                .CreateDrawer<GridOutlineRenderSystem>("Grid/Outline");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig>())
            {
                if (!global.Enabled) continue;

                var converter = new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize,
                    visualizer.GridWidth, visualizer.GridHeight);

                var min = converter.GridMin;
                var max = converter.GridMax;
                var y = min.y + 0.01f;

                drawer.Line(new float3(min.x, y, min.z), new float3(max.x, y, min.z), GridPalette.GridBorder);
                drawer.Line(new float3(max.x, y, min.z), new float3(max.x, y, max.z), GridPalette.GridBorder);
                drawer.Line(new float3(max.x, y, max.z), new float3(min.x, y, max.z), GridPalette.GridBorder);
                drawer.Line(new float3(min.x, y, max.z), new float3(min.x, y, min.z), GridPalette.GridBorder);

                if (!config.DrawGrid) continue;

                for (var x = 0; x <= converter.Width; x++)
                {
                    var x0 = min.x + x * converter.CellSize;
                    drawer.Line(new float3(x0, y, min.z), new float3(x0, y, max.z), GridPalette.GridLine);
                }

                for (var z = 0; z <= converter.Height; z++)
                {
                    var z0 = min.z + z * converter.CellSize;
                    drawer.Line(new float3(min.x, y, z0), new float3(max.x, y, z0), GridPalette.GridLine);
                }
            }
        }
    }
}
#endif