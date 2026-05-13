using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridConflictRenderSystem : ISystem
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
                SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<GridConflictRenderSystem>("Grid/Conflicts");
            if (!drawer.IsEnabled) return;

            foreach (var (visualizer, global, config, conflicts) in
                     SystemAPI.Query<GridVisualizerData, GridVisualizerGlobal, GridAlgorithmVisualConfig,
                         DynamicBuffer<GridConflictVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawConflicts) continue;

                var converter =
                    new GridCoordinateConverter(visualizer.Origin, visualizer.CellSize, visualizer.GridWidth,
                        visualizer.GridHeight);

                var array = conflicts.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var c = array[i];
                    if (config.DrawTimeline && c.Time != global.CurrentFrame) continue;
                    if (global.Mode == GridVisualizerMode.Step && c.Time > global.CurrentFrame) continue;

                    var center = converter.CellCenter(c.Cell);
                    center.y += 0.3f;

                    var size = new float3(converter.CellSize, 0.05f, converter.CellSize);
                    drawer.Cuboid(center, quaternion.identity, size, GridPalette.Conflict);

                    var text = new FixedString64Bytes();
                    text.Append('A');
                    text.Append(c.AgentA);
                    text.Append('<');
                    text.Append('-');
                    text.Append('>');
                    text.Append('A');
                    text.Append(c.AgentB);
                    text.Append(' ');
                    text.Append('t');
                    text.Append('=');
                    text.Append(c.Time);

                    center.y += 0.4f;
                    drawer.Text64(center, text, GridPalette.Conflict, 10f);
                }
            }
        }
    }
}