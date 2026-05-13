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
    public partial struct GridConstraintRenderSystem : ISystem
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
                    .CreateDrawer<GridConstraintRenderSystem>("Grid/Constraints");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, constraints) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridConstraintVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawConstraints) continue;

                var converter =
                    new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize, visualizer.GridWidth,
                        visualizer.GridHeight);

                var array = constraints.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var c = array[i];
                    if (config.DrawTimeline && c.Time != global.CurrentFrame) continue;
                    if (global.Mode == GridVisualizerMode.Step && c.Time > global.CurrentFrame) continue;

                    var center = converter.CellCenter(c.Cell);
                    center.y += 0.08f;

                    var color = GridPalette.AgentColor(c.Agent);
                    color.a = 0.5f;
                    var size = new float3(converter.CellSize * 0.9f, 0.03f, converter.CellSize * 0.9f);

                    drawer.Cuboid(center, quaternion.identity, size, color);

                    center.y += 0.15f;
                    drawer.Point(center, 2f, GridPalette.Constraint);
                }
            }
        }
    }
}
