using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation |
                       WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation |
                       WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    [UpdateAfter(typeof(GridCellVisualStateTransitionSystem))]
    public partial struct GridPillarRenderSystem : ISystem
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
                .CreateDrawer<GridPillarRenderSystem>("Grid/Pillars");

            if (!drawer.IsEnabled)
            {
                return;
            }

            foreach (var (visualizer, global, style, pillar, states) in
                     SystemAPI.Query<
                         GridVisualizerData,
                         GridVisualizerGlobal,
                         GridVisualizerStyle,
                         GridPillarStyleConfig,
                         DynamicBuffer<GridCellVisualState>>())
            {
                if (!global.Enabled)
                {
                    continue;
                }

                if (style.Type != GridVisualStyleType.Pillar)
                {
                    continue;
                }

                var width = visualizer.GridWidth;
                var height = visualizer.GridHeight;
                var count = width * height;

                if (states.Length < count)
                {
                    continue;
                }

                var converter = new GridCoordinateConverter(
                    visualizer.Origin,
                    visualizer.CellSize,
                    visualizer.GridWidth,
                    visualizer.GridHeight);

                for (var index = 0; index < count; index++)
                {
                    var stateValue = states[index];

                    var center = converter.CellCenter(index);
                    var heightValue = math.max(0.02f, pillar.BaseHeight - stateValue.CurrentDepth);

                    center.y += heightValue * 0.5f;

                    var size = new float3(
                        visualizer.CellSize * pillar.BlockSize.x,
                        heightValue,
                        visualizer.CellSize * pillar.BlockSize.z);

                    var fill = ToColor(stateValue.CurrentColor);
                    var outline = ToColor(pillar.OutlineColor);

                    drawer.Cuboid(center, quaternion.identity, size, fill);

                    var top = center;
                    top.y += heightValue * 0.5f;
                    DrawTopOutline(ref drawer, top, size, outline);
                }
            }
        }

        private static void DrawTopOutline(ref Drawer drawer, float3 center, float3 size, Color color)
        {
            var ex = size.x * 0.5f;
            var ez = size.z * 0.5f;

            var p0 = center + new float3(-ex, 0f, -ez);
            var p1 = center + new float3(-ex, 0f, ez);
            var p2 = center + new float3(ex, 0f, ez);
            var p3 = center + new float3(ex, 0f, -ez);

            drawer.Line(p0, p1, color);
            drawer.Line(p1, p2, color);
            drawer.Line(p2, p3, color);
            drawer.Line(p3, p0, color);
        }

        private static Color ToColor(float4 color)
        {
            return new Color(color.x, color.y, color.z, color.w);
        }
    }
}
