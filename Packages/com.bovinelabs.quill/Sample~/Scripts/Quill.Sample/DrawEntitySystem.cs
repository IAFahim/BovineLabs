// <copyright file="DrawEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Sample
{
    using BovineLabs.Quill;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;

    public partial struct DrawEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer<DrawEntitySystem>();

            if (!drawer.IsEnabled)
            {
                return;
            }

            new DrawJob { Drawer = drawer }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct DrawJob : IJobEntity
        {
            public Drawer Drawer;

            private void Execute(Entity entity, in LocalTransform localTransform)
            {
                this.Drawer.Text64(localTransform.Position + math.up(), entity.ToFixedString(), Color.red);
            }
        }
    }
}
