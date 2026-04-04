// <copyright file="SphereDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct SphereDrawer : IDrawer
    {
        public float3 Center;
        public float Radius;
        public int SideCount;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            var sideCount = math.clamp(this.SideCount, 3, 32);
            var arcStep = (2f * math.PI) / sideCount;

            var pa = math.cos(arcStep * (sideCount - 1)) * this.Radius;
            var pb = math.sin(arcStep * (sideCount - 1)) * this.Radius;

            var px = new float3(0, pa, pb) + this.Center;
            var py = new float3(pa, 0, pb) + this.Center;
            var pz = new float3(pa, pb, 0) + this.Center;

            for (var i = 0; i < sideCount; i++)
            {
                var a = math.cos(arcStep * i) * this.Radius;
                var b = math.sin(arcStep * i) * this.Radius;

                var vx = new float3(0, a, b) + this.Center;
                var vy = new float3(a, 0, b) + this.Center;
                var vz = new float3(a, b, 0) + this.Center;

                builder.DrawLine(vx, px, this.Color);
                builder.DrawLine(vy, py, this.Color);
                builder.DrawLine(vz, pz, this.Color);

                px = vx;
                py = vy;
                pz = vz;
            }
        }
    }
}
