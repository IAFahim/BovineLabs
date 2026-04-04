// <copyright file="SectorDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct SectorDrawer : IDrawer
    {
        public float3 Point;
        public float3 Forward;
        public float3 Up;
        public float Radius;
        public float Angle;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            // Draw the leftmost boundary
            var halfAngle = this.Angle / 2.0f;
            var arm = math.mul(quaternion.AxisAngle(this.Up, -halfAngle), this.Forward) * this.Radius;
            builder.DrawLine(this.Point, this.Point + arm, this.Color);

            // Draw the arc
            const int res = 16;
            var q = quaternion.AxisAngle(this.Up, this.Angle / res);
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, arm);
                builder.DrawLine(this.Point + arm, this.Point + nextArm, this.Color);
                arm = nextArm;
            }

            // Draw the rightmost boundary
            builder.DrawLine(this.Point + arm, this.Point, this.Color);
        }
    }
}
