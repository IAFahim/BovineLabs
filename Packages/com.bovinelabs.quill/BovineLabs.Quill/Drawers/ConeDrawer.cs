// <copyright file="ConeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using BovineLabs.Core.Utility;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct ConeDrawer : IDrawer
    {
        public float3 Point;
        public float3 Direction;
        public float Angle;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            var scale = mathex.NormalizeWithLength(this.Direction, out var dir);

            // we don't want the scale to equal the arm length, instead of we want it to equal the projection length
            // this means changing angle won't change how far the cone draws out
            scale /= math.cos(this.Angle);

            mathex.CalculatePerpendicularNormalized(dir, out var perp, out _);
            var arm = math.mul(quaternion.AxisAngle(perp, this.Angle), dir * scale);

            const int res = 16;
            var q = quaternion.AxisAngle(dir, (2.0f * math.PI) / res);
            for (var i = 0; i < res; i++)
            {
                var nextArm = math.mul(q, arm);
                builder.DrawLine(this.Point, this.Point + arm, this.Color);
                builder.DrawLine(this.Point + arm, this.Point + nextArm, this.Color);
                arm = nextArm;
            }
        }
    }
}
