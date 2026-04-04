// <copyright file="CylinderDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct CylinderDrawer : IDrawer
    {
        public float3 Center;
        public quaternion Rotation;
        public float Height;
        public float Radius;
        public int SideCount;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            var transform = new RigidTransform(this.Rotation, this.Center);
            var halfHeight = this.Height / 2f;

            var sideCount = math.clamp(this.SideCount * 20, 3, 32);
            var arcStep = (2f * math.PI) / sideCount;
            var straights = sideCount / 4;

            var px = math.cos(arcStep * (sideCount - 1)) * this.Radius;
            var py = math.sin(arcStep * (sideCount - 1)) * this.Radius;

            var p0 = math.transform(transform, new float3(px, py, -halfHeight));
            var p1 = math.transform(transform, new float3(px, py, halfHeight));

            for (var i = 0; i < sideCount; i++)
            {
                var x = math.cos(arcStep * i) * this.Radius;
                var y = math.sin(arcStep * i) * this.Radius;

                var v0 = math.transform(transform, new float3(x, y, -halfHeight));
                var v1 = math.transform(transform, new float3(x, y, halfHeight));

                if (i % straights == 0)
                {
                    builder.DrawLine(v0, v1, this.Color); // vert
                }

                builder.DrawLine(v0, p0, this.Color);
                builder.DrawLine(v1, p1, this.Color);

                p0 = v0;
                p1 = v1;
            }
        }
    }
}
