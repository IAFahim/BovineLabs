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
    public partial struct GridTextRenderSystem : ISystem
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
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<GridTextRenderSystem>("Grid/Text");
            if (!drawer.IsEnabled) return;

            foreach (var (global, config, texts) in
                     SystemAPI.Query<GridVisualizerGlobal, GridAlgorithmVisualConfig, DynamicBuffer<GridTextVisual>>())
            {
                if (!global.Enabled) continue;
                if (!config.DrawLabels && !config.DrawMessages) continue;

                var array = texts.AsNativeArray();
                for (var i = 0; i < array.Length; i++)
                {
                    var t = array[i];
                    if (global.Mode == GridVisualizerMode.Step && t.Frame > global.CurrentFrame) continue;
                    if (config.DrawTimeline && t.Frame != global.CurrentFrame) continue;

                    var color = new Color(t.Color.x, t.Color.y, t.Color.z, t.Color.w);
                    drawer.Text64(t.Position, t.Text, color, 12f);
                }
            }
        }
    }
}
#endif