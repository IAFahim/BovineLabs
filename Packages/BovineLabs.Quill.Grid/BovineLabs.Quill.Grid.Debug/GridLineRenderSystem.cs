using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridLineRenderSystem : ISystem
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
                .CreateDrawer<GridLineRenderSystem>("Grid/Lines");
            if (!drawer.IsEnabled) return;

            foreach (var (global, lines) in SystemAPI.Query<GridVisualizerGlobal, DynamicBuffer<GridLineVisual>>())
            {
                if (!global.Enabled) continue;

                var array = lines.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var line = array[i];
                    if (global.Mode == GridVisualizerMode.Step && line.Frame > global.CurrentFrame) continue;

                    var color = new Color(line.Color.x, line.Color.y, line.Color.z, line.Color.w);
                    if (line.Frame != global.CurrentFrame)
                        color.a *= 0.35f;

                    drawer.Line(line.From, line.To, color);
                }
            }
        }
    }
}
#endif