// <copyright file="DrawCubeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Sample
{
    using BovineLabs.Quill;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public partial struct DrawCubeSystem : ISystem
    {
        private NativeReference<quaternion> transform;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.transform = new NativeReference<quaternion>(Allocator.Persistent);
            this.transform.Value = quaternion.identity;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.transform.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new DrawJob { Transform = this.transform, DeltaTime = SystemAPI.Time.DeltaTime }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct DrawJob : IJob
        {
            public NativeReference<quaternion> Transform;

            public float DeltaTime;

            public void Execute()
            {
                var rotation = new float3(this.DeltaTime);
                this.Transform.Value = math.mul(this.Transform.Value, quaternion.EulerXYZ(rotation));

                GlobalDraw.Cuboid(new float3(5, 0, 0),  this.Transform.Value, new float3(1), Color.blue);
            }
        }
    }
}
