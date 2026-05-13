using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridAgentPathRenderSystem : ISystem
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
                    .CreateDrawer<GridAgentPathRenderSystem>("Grid/AgentPaths");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, agents) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridAgentPathVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawPath) continue;

                var converter =
                    new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize, visualizer.GridWidth,
                        visualizer.GridHeight);

                var array = agents.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var curr = array[i];
                    if (global.Mode == GridVisualizerMode.Step && curr.TimeStep > global.CurrentFrame) continue;

                    var color = GridPalette.AgentColor(curr.AgentIndex);
                    if (config.DrawTimeline && curr.TimeStep != global.CurrentFrame)
                        color.a = 0.25f;

                    var pos = converter.CellCenter(curr.Cell);
                    pos.y += 0.25f;
                    drawer.Point(pos, 3f, color);

                    if (i == 0) continue;

                    var prev = array[i - 1];
                    if (prev.AgentIndex != curr.AgentIndex || prev.TimeStep + 1 != curr.TimeStep)
                        continue;

                    var from = converter.CellCenter(prev.Cell);
                    var to = converter.CellCenter(curr.Cell);
                    from.y += 0.25f;
                    to.y += 0.25f;

                    drawer.Line(from, to, color);
                }
            }
        }
    }
}
