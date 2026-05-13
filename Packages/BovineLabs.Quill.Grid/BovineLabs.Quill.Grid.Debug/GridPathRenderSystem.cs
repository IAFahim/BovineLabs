using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridPathRenderSystem : ISystem
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
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<GridPathRenderSystem>("Grid/Path");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, segments) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridPathVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawPath) continue;

                var converter = new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize,
                    visualizer.GridWidth, visualizer.GridHeight);

                var array = segments.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var seg = array[i];
                    if (global.Mode == GridVisualizerMode.Step && seg.Frame > global.CurrentFrame) continue;
                    if (config.DrawTimeline && seg.Frame != global.CurrentFrame) continue;

                    var from = converter.CellCenter(seg.From);
                    var to = converter.CellCenter(seg.To);
                    from.y += 0.2f;
                    to.y += 0.2f;

                    drawer.Line(from, to, GridPalette.Path);
                    drawer.Point(from, 3f, GridPalette.Path);
                }
            }
        }
    }
}
#endif
