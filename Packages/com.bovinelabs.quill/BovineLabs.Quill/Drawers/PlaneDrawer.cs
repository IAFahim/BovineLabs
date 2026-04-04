// <copyright file="PlaneDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using BovineLabs.Core.Utility;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct PlaneDrawer : IDrawer
    {
        public float3 Center;
        public float3 Direction;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            if (!this.Direction.Equals(float3.zero))
            {
                var length = mathex.NormalizeWithLength(this.Direction, out var dir);
                mathex.CalculatePerpendicularNormalized(dir, out var perp, out var perp2);

                perp *= length;
                perp2 *= length;
                builder.DrawLine(this.Center + perp + perp2, (this.Center + perp) - perp2, this.Color);
                builder.DrawLine((this.Center + perp) - perp2, this.Center - perp - perp2, this.Color);
                builder.DrawLine(this.Center - perp - perp2, (this.Center - perp) + perp2, this.Color);
                builder.DrawLine((this.Center - perp) + perp2, this.Center + perp + perp2, this.Color);
            }
        }
    }
}
