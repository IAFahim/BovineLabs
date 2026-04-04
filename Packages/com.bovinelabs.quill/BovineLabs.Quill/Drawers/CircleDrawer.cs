// <copyright file="CircleDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using BovineLabs.Core.Utility;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct CircleDrawer : IDrawer
    {
        public float3 Center;
        public float3 Direction;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            if (this.Direction.Equals(float3.zero))
            {
                return;
            }

            var length = mathex.NormalizeWithLength(this.Direction, out var dir);
            mathex.CalculatePerpendicularNormalized(dir, out var perp, out _);

            const int res = 16;
            var q = quaternion.AxisAngle(dir, (2.0f * math.PI) / res);
            var arm = perp * length;
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, arm);
                builder.DrawLine(this.Center + arm, this.Center + nextArm, this.Color);
                arm = nextArm;
            }
        }
    }
}
