// <copyright file="CapsuleDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct CapsuleDrawer : IDrawer
    {
        public float3 Center;
        public quaternion Rotation;
        public float Height;
        public float Radius;
        public int SideCount;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            const int radiusSideCount = 8;

            var transform = new RigidTransform(this.Rotation, this.Center);

            var halfHeight = (this.Height / 2f) - this.Radius;

            var sideCount = math.clamp(this.SideCount * 20, 3, 32);
            var arcStep = (2f * math.PI) / sideCount;
            var arcStepRadius = math.PI / 2f / radiusSideCount;
            var straights = sideCount / 4;

            var px = math.cos(arcStep * (sideCount - 1)) * this.Radius;
            var py = math.sin(arcStep * (sideCount - 1)) * this.Radius;

            var p0 = math.transform(transform, new float3(px, -halfHeight, py));
            var p1 = math.transform(transform, new float3(px, halfHeight, py));

            for (var i = 0; i < sideCount; i++)
            {
                var x = math.cos(arcStep * i) * this.Radius;
                var z = math.sin(arcStep * i) * this.Radius;

                var v0 = math.transform(transform, new float3(x, -halfHeight, z));
                var v1 = math.transform(transform, new float3(x, halfHeight, z));

                builder.DrawLine(v0, p0, this.Color);
                builder.DrawLine(v1, p1, this.Color);

                p0 = v0;
                p1 = v1;

                // We only draw the straight sections every quarter
                if (i % straights == 0)
                {
                    // Vertical line
                    builder.DrawLine(v0, v1, this.Color);

                    var v2 = v0;
                    var v3 = v1;
                    var direction = math.normalize(new float3(x, 0, z));

                    // Circle arcs
                    for (var j = 0; j <= radiusSideCount; j++)
                    {
                        var xz = direction * (math.cos(arcStepRadius * j) * this.Radius);
                        var yy = halfHeight + (math.sin(arcStepRadius * j) * this.Radius);

                        var u0 = math.transform(transform, xz + new float3(0, -yy, 0));
                        var u1 = math.transform(transform, xz + new float3(0, yy, 0));
                        builder.DrawLine(v2, u0, this.Color);
                        builder.DrawLine(v3, u1, this.Color);
                        v2 = u0;
                        v3 = u1;
                    }
                }
            }
        }
    }
}
