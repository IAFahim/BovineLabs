// <copyright file="CuboidDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct CuboidDrawer : IDrawer
    {
        public float3 Center;
        public quaternion Rotation;
        public float3 Size;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            var halfSize = this.Size / 2f;

            var min = -halfSize;
            var max = +halfSize;

            var t = new RigidTransform(this.Rotation, this.Center);

            var a = math.transform(t, new float3(min.x, min.y, min.z));
            var b = math.transform(t, new float3(min.x, max.y, min.z));
            var c = math.transform(t, new float3(max.x, max.y, min.z));
            var d = math.transform(t, new float3(max.x, min.y, min.z));

            var e = math.transform(t, new float3(min.x, min.y, max.z));
            var f = math.transform(t, new float3(min.x, max.y, max.z));
            var g = math.transform(t, new float3(max.x, max.y, max.z));
            var h = math.transform(t, new float3(max.x, min.y, max.z));

            // Front
            builder.DrawLine(a, b, this.Color);
            builder.DrawLine(b, c, this.Color);
            builder.DrawLine(c, d, this.Color);
            builder.DrawLine(d, a, this.Color);

            // Back
            builder.DrawLine(e, f, this.Color);
            builder.DrawLine(f, g, this.Color);
            builder.DrawLine(g, h, this.Color);
            builder.DrawLine(h, e, this.Color);

            // Left
            builder.DrawLine(a, e, this.Color);
            builder.DrawLine(b, f, this.Color);

            // Right
            builder.DrawLine(c, g, this.Color);
            builder.DrawLine(d, h, this.Color);
        }
    }
}
