// <copyright file="DrawAlwaysSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Sample
{
    using BovineLabs.Quill;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public partial struct DrawAlwaysSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();
            state.Dependency = new DrawJob { Drawer = drawer }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct DrawJob : IJob
        {
            public Drawer Drawer;

            public void Execute()
            {
                this.Drawer.Text128(new float3(0, 3, 0), "Enable:\n- BovineLabs Menu->ConfigVars draw.enable-global\n- Toolbar->Default->Draw->Draw Entity", Color.white, 32);
            }
        }
    }
}
