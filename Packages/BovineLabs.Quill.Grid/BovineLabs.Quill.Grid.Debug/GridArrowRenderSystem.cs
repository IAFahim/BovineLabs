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
    public partial struct GridArrowRenderSystem : ISystem
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
                SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<GridArrowRenderSystem>("Grid/Arrows");
            if (!drawer.IsEnabled) return;

            foreach (var (global, arrows) in SystemAPI.Query<GridVisualizerGlobal, DynamicBuffer<GridArrowVisual>>())
            {
                if (!global.Enabled) continue;

                var array = arrows.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var a = array[i];
                    if (global.Mode == GridVisualizerMode.Step && a.Frame > global.CurrentFrame) continue;

                    var color = new Color(a.Color.x, a.Color.y, a.Color.z, a.Color.w);
                    if (a.Frame != global.CurrentFrame)
                        color.a *= 0.35f;

                    drawer.Arrow(a.From, a.To - a.From, color);
                }
            }
        }
    }
}