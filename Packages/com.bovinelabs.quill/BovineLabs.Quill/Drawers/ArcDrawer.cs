// <copyright file="ArcDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct ArcDrawer : IDrawer
    {
        public float3 Center;
        public float3 Normal;
        public float3 Arm;
        public float Angle;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            const int res = 16;
            var q = quaternion.AxisAngle(this.Normal, this.Angle / res);
            var currentArm = this.Arm;
            builder.DrawLine(this.Center, this.Center + currentArm, this.Color);
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, currentArm);
                builder.DrawLine(this.Center + currentArm, this.Center + nextArm, this.Color);
                currentArm = nextArm;
            }

            builder.DrawLine(this.Center, this.Center + currentArm, this.Color);
        }
    }
}
